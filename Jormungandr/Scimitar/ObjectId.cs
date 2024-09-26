using System.Runtime.InteropServices;

namespace Jormungandr.Scimitar;

// note: is there some sort of structure to this?
[StructLayout(LayoutKind.Sequential, Size = 8)]
public readonly record struct ObjectId(ulong Value) {
	public override string ToString() => $"0x{LocalId:x12}:0x{ExportId:X6}";

	public int PrincipalId => (int) (Value & 0xFFFF);
	public int LocalId => (int) (Value & 0x7FFFFFFFFFF);
	public int ExportId => (int) (Value >> 43);
}
