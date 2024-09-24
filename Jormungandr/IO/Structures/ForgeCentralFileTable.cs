using System.Runtime.InteropServices;
using Jormungandr.Scimitar;

namespace Jormungandr.IO.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct ForgeCentralFileTable {
	public int TotalFileCount { get; init; }
	public int TotalDirectoryCount { get; init; }
	public ObjectId MaxId { get; init; }
	public uint RootId { get; init; }
	public int FirstFreeFile { get; init; }
	public int FirstFreeDir { get; init; }
	public int SizeOfFileTable { get; init; }
	public int FileTableCount { get; init; }
	public long FileTableOffset { get; init; }
}
