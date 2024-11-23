using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Jormungandr.Cryptography;
using Jormungandr.IO.Buffers;
using Jormungandr.IO.Structures;
using Jormungandr.Scimitar;

namespace Jormungandr.IO;

public sealed class MemoryReader(RentedMemory<byte> Buffer, bool DisposeOnExit = true) : IDisposable {
	private bool DisposeOnExit { get; } = DisposeOnExit;
	public RentedMemory<byte> Buffer { get; } = Buffer;
	public int Position { get; set; }
	public int Length => Buffer.Length;

	public void Dispose() {
		if (DisposeOnExit) {
			Buffer.Dispose();
		}
	}

	public byte ReadByte() => Buffer.Memory.Span[Position++];

	public bool ReadFlag() {
		var value = Buffer.Memory.Span[Position++];
		Debug.Assert(value is 0 or 1);
		return value == 1;
	}

	public Memory<byte> ReadBytes(int count) {
		var value = Buffer.Memory.Slice(Position, count);
		Position += count;
		return value;
	}

	public T Peek<T>() where T : struct => MemoryMarshal.Read<T>(Buffer.Memory.Span[Position..]);

	public Memory<T> ReadArray<T>(int count) where T : struct {
		var size = Unsafe.SizeOf<T>() * count;
		var value = Buffer.Memory.Slice(Position, size);
		Position += size;
		return new MemoryTypeManager<T, byte>(value).Memory;
	}

	public T Read<T>() where T : struct {
		var value = Peek<T>();
		Position += Unsafe.SizeOf<T>();
		return value;
	}

	public Memory<T> ReadArray<T>() where T : struct => ReadArray<T>(Read<int>());

	public string ReadString() {
		var size = Read<int>();
		var bytes = ReadBytes(size);
		return Encoding.UTF8.GetString(bytes.Span);
	}

	public string ReadString(uint tag, ObjectId uid, out bool isEncrypted) {
		var size = Read<int>();
		var flags = (StringFlags) (size >> 29);
		size &= 0x1fffffff;
		if (size == 0) {
			isEncrypted = false;
			return string.Empty;
		}

		var isUtf16 = flags.HasFlagFast(StringFlags.UTF16);
		Trace.Assert(!isUtf16);
		if (isUtf16) {
			size *= 2;
		}

		isEncrypted = flags.HasFlagFast(StringFlags.BlockEncrypted);
		if (isEncrypted) {
			// align to 16 bytes
			isEncrypted = true;
			size = (int) ((size + 0xf) & 0xfffffff0L);
			// can't do decryption as the encryption key is not shipped
		}

		var bytes = ReadBytes(size);
		if (flags.HasFlagFast(StringFlags.StepEncrypted)) {
			Debugger.Break();
			StepEncoder.Decode<StepEncoder.ACK>(bytes.Span, tag);
		}

		return isEncrypted
			? Convert.ToHexString(bytes.Span)
			: (isUtf16
				? Encoding.Unicode
				: Encoding.UTF8)
		   .GetString(bytes.Span[..size]);
	}
}
