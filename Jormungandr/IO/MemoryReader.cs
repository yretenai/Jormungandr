using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Jormungandr.IO.Buffers;
using Jormungandr.IO.Structures;

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

	public string ReadString(out bool isEncrypted) {
		var size = Read<int>();
		var flags = (StringFlags) (size >> 29);
		size &= 0x1fffffff;

		var isUtf16 = flags.HasFlagFast(StringFlags.UTF16);
		Debug.Assert(!isUtf16);
		if (isUtf16) {
			size *= 2;
		}

		if (flags.HasFlagFast(StringFlags.BlockEncrypted)) { // align to 16 bytes
			isEncrypted = true;
			size = (int) (size & 0xfffffff0L) + 16;
		} else if (flags.HasFlagFast(StringFlags.StepEncrypted)) { // align to 8 bytes
			isEncrypted = true;
			size = (int) (size & 0xfffffff8L) + 8;
		} else {
			isEncrypted = false;
		}

		var bytes = ReadArray<byte>(size);

		Position += 1;

		if (isUtf16) {
			Position += 1;
		}

		return isEncrypted
			? Convert.ToHexString(bytes.Span)
			: (isUtf16
				? Encoding.Unicode
				: Encoding.UTF8)
		   .GetString(bytes.Span);
	}
}
