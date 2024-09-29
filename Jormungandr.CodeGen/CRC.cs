using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Jormungandr.CodeGen;

public sealed class CRC : HashAlgorithm {
	private readonly uint Init;
	private readonly uint Polynomial;
	private readonly bool ReflectIn;
	private readonly bool ReflectOut;
	private readonly uint[] Table;
	private readonly uint Xor;
	public uint Value { get; set; }

	public CRC(uint polynomial, uint init, uint xor, bool reflectIn, bool reflectOut) {
		Polynomial = polynomial;
		HashSizeValue = sizeof(uint) * 8;
		Init = init;
		Xor = xor;
		ReflectIn = reflectIn;
		ReflectOut = reflectOut;
		Table = new uint[256];

		Reset();
		CreateTable();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	private void CreateTable() {
		const int WIDTH = 0x20;
		const ulong MSB = 1ul << (WIDTH - 1);
		const int ALIGN = WIDTH - 8;

		for (var i = 0u; i < 256; ++i) {
			var r = i;

			if (ReflectIn) {
				r = Reflect(r, WIDTH);
			} else if (WIDTH > 8) {
				r <<= ALIGN;
			}

			for (var j = 0; j < 8; j++) {
				if ((r & MSB) != 0) {
					r = (r << 1) ^ Polynomial;
				} else {
					r <<= 1;
				}
			}

			if (ReflectIn) {
				r = Reflect(r, WIDTH);
			}

			Table[i] = r;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	protected override void HashCore(byte[] array, int ibStart, int cbSize) {
		while (cbSize > 0) {
			var @byte = uint.CreateTruncating(array[ibStart++]);
			if (ReflectOut) {
				Value = Table[(Value ^ @byte) & 0xFF] ^ (Value >> 8);
			} else {
				Value = Table[((Value >> (sizeof(uint) * 8 - 8)) ^ @byte) & 0xFF] ^ (Value << 8);
			}

			cbSize--;
		}
	}

	public uint GetValueFinal() {
		var val = Value ^ Xor;
		Reset();
		return val;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Reset() => Value = Init;

	protected override byte[] HashFinal() {
		Span<uint> tmp = stackalloc uint[1];
		tmp[0] = GetValueFinal();
		Reset();
		return MemoryMarshal.AsBytes(tmp).ToArray();
	}

	public uint ComputeHash(string text) {
		Span<byte> bytes = stackalloc byte[Encoding.ASCII.GetMaxByteCount(text.Length)];
		var n = Encoding.ASCII.GetBytes(text, bytes);
		return ComputeHash(bytes[..n]);
	}

	public uint ComputeHash(ReadOnlySpan<byte> bytes) {
		HashCore(bytes);
		return GetValueFinal();
	}

	public override void Initialize() => Reset();

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static uint Reflect(uint v, int width) {
		v = ((v >> 1) & 0x55555555) | ((v & 0x55555555) << 1);
		v = ((v >> 2) & 0x33333333) | ((v & 0x33333333) << 2);
		v = ((v >> 4) & 0x0F0F0F0F) | ((v & 0x0F0F0F0F) << 4);
		v = ((v >> 8) & 0x00FF00FF) | ((v & 0x00FF00FF) << 8);
		v = ((v >> 16) & 0x0000FFFF) | ((v & 0x0000FFFF) << 16);
		return v;
	}
}
