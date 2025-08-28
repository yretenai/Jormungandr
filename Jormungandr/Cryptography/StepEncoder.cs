using System.Numerics;
using System.Runtime.InteropServices;

namespace Jormungandr.Cryptography;

public static class StepEncoder {
	public const ulong Xor = 0xb6539797901776ec;
	public const ulong Step = 0xf3f1410c3eb549db;

	// This is disastrous code.
	public static void Decode(Span<byte> bytes, uint tag, KeyRing keyRing) {
		// Stage 1: Project-Specific key tweaking
		Span<ulong> data = stackalloc ulong[(1 + bytes.Length) >> 3];
		bytes.CopyTo(MemoryMarshal.AsBytes(data));

		var akey = keyRing.EKey;
		var ckey = keyRing.CKey;
		var key = ((ulong) tag << 32) + tag; // equivalent to * 0x100000001

		key += akey;

		for (var i = 0; i < data.Length; ++i) {
			data[i] ^= key;
			key += ckey;
		}

		MemoryMarshal.AsBytes(data).CopyTo(bytes);

		// Stage 2: Rotate right by 11, and Add
		int cursor;
		unchecked {
			for (cursor = 0; cursor + 8 < bytes.Length; cursor += 8) {
				var value = MemoryMarshal.Read<ulong>(bytes[cursor..]);
				MemoryMarshal.Write(bytes[cursor..], BitOperations.RotateRight(value, 11) + Step);
			}

			for (; cursor + 4 < bytes.Length; cursor += 4) {
				var value = MemoryMarshal.Read<uint>(bytes[cursor..]);
				MemoryMarshal.Write(bytes[cursor..], BitOperations.RotateRight(value, 11) + (uint) Step);
			}

			for (; cursor + 2 < bytes.Length; cursor += 2) {
				var value = MemoryMarshal.Read<ushort>(bytes[cursor..]);
				MemoryMarshal.Write(bytes[cursor..], (ushort) ((value >> 11) | (value << 5)) + (ushort) Step);
			}

			for (; cursor < bytes.Length; cursor += 1) {
				var value = (ulong) bytes[cursor];
				bytes[cursor] = (byte) ((byte) ((value >> 3) | (value << 5)) + (byte) Step); // 11 % 8 = 3
			}
		}

		// Stage 3: XOR
		for (cursor = 0; cursor < bytes.Length; cursor += 1) {
			var value = bytes[cursor];
			bytes[cursor] = (byte) (value ^ (byte) (Xor >> (cursor & 0x3f)));
		}
	}
}
