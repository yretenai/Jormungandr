using System.Runtime.InteropServices;

namespace Jormungandr.Scimitar;

// note: is there some sort of structure to this?
[StructLayout(LayoutKind.Sequential, Size = 8)]
public readonly record struct ObjectId(ulong Value) {
	public override string ToString() => "0x" + Value.ToString("x16");
}
