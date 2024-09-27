namespace Jormungandr.IO.Structures;

[Flags]
public enum StringFlags {
	UTF16 = 1, // Guessed. Need to validate. (0x20000000)
	BlockEncrypted = 2, // AES-like (0x40000000)
	StepEncrypted = 4, // XOR with constants (0x80000000)
}

public static class StringFlagsExtensions {
	public static bool HasFlagFast(this StringFlags value, StringFlags flag) => (value & flag) != 0;
}
