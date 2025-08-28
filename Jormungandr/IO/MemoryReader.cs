using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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

	public string ReadString(uint tag, ObjectId uid, out bool isEncrypted, KeyRing? keyRing) {
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

		var textLength = size;

		isEncrypted = flags.HasFlagFast(StringFlags.StepEncrypted) || flags.HasFlagFast(StringFlags.BlockEncrypted);
		if (isEncrypted) {
			if (flags.HasFlagFast(StringFlags.StepEncrypted)) {
				// align to 8 bytes
				size = (int) ((size + 0x7) & 0xfffffff8L);
			} else if (flags.HasFlagFast(StringFlags.BlockEncrypted)) {
				size = (int) ((size + 0xf) & 0xfffffff0L);
			}
		}

		var bytes = ReadBytes(size);

		if (keyRing is not null && isEncrypted) {
			if (flags.HasFlagFast(StringFlags.StepEncrypted) && keyRing is { CKey: > 0, EKey: > 0 }) {
				Debugger.Break();
				StepEncoder.Decode(bytes.Span, tag, keyRing);
				isEncrypted = false;
			} else if (flags.HasFlagFast(StringFlags.BlockEncrypted) && keyRing is { AKey.Length: > 0, IKey.Length: > 0 }) {
				using var aes = Aes.Create();
				aes.Key = keyRing.AKey;
				aes.DecryptCbc(bytes.Span, keyRing.IKey, bytes.Span, PaddingMode.None); // actually PKCS7 but not if it's on a boundary.
				isEncrypted = false;
			}
		}

		return isEncrypted
			? Convert.ToHexString(bytes.Span)
			: (isUtf16
				? Encoding.Unicode
				: Encoding.UTF8)
			.GetString(bytes.Span[..textLength]);
	}
}
