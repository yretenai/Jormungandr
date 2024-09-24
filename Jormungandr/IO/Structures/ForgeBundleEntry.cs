using System.Runtime.InteropServices;
using Jormungandr.Scimitar;

namespace Jormungandr.IO.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct ForgeBundleEntry {
	public ushort Flags { get; init; }
	public ObjectId ObjectId { get; init; }
	public int Size { get; init; }
}
