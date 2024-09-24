using System.Runtime.InteropServices;
using Jormungandr.Scimitar;

namespace Jormungandr.IO.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct ForgeHeader {
	public StaticArray8 Magic { get; init; }
	internal byte Padding { get; init; }
	public uint Version { get; init; }
	public long FileAllocationTableOffset { get; init; }
	public ObjectId GlobalManifestObjectId { get; init; }
	public ulong Flags { get; init; }
}
