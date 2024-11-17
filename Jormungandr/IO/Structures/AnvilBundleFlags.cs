namespace Jormungandr.IO.Structures;

[Flags]
public enum AnvilBundleFlags : uint {
	Tagged = 0x40000,
	Serialized = 0x80000000,
}
