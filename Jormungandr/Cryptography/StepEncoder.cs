using System.Runtime.InteropServices;

namespace Jormungandr.Cryptography;

// https://github.com/parzivail/RainbowForge/blob/master/RainbowForge/Core/NameEncoding.cs
public static class StepEncoder {
	public const ulong FILENAME_ENCODING_BASE_KEY = 0xA860F0ECDE3339FB;
	public const ulong FILENAME_ENCODING_ENTRY_KEY_STEP = 0x357267C76FFB9EB2;
	public const ulong FILENAME_ENCODING_FILE_KEY_STEP = 0xE684BFF857699452;

	private static ulong CalculateLengthTweak(ulong length) {
		var lowerPart = length * 0x421;
		var upperPart = (((length & 0xF) * 0x21) & 0x7F) + (length & 0xF0) << 25;
		return lowerPart | upperPart;
	}

	public static void Decode(Span<byte> bytes, int length, uint tag, ulong uid) {
		var key = FILENAME_ENCODING_BASE_KEY + tag + ((ulong) tag << 32) + CalculateLengthTweak((ulong) length);
		var aligned = (int) (length & 0xfffffff8L) + 8;
		Span<byte> tmp = stackalloc byte[aligned];
		bytes.CopyTo(tmp);

		var cipher = MemoryMarshal.Cast<byte, ulong>(tmp);
		for (var i = 0; i < aligned >> 3; i++) {
			cipher[i] ^= key;
			key += FILENAME_ENCODING_FILE_KEY_STEP;
		}

		tmp[..length].CopyTo(bytes);
	}
}
