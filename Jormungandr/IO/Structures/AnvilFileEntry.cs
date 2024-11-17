using System.Runtime.InteropServices;
using Jormungandr.Scimitar;

namespace Jormungandr.IO.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct AnvilFileEntry {
	public long Offset { get; init; }
	public ObjectId Id { get; init; }
	public int Size { get; init; }
}
