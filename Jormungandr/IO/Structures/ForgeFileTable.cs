using System.Runtime.InteropServices;

namespace Jormungandr.IO.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct ForgeFileTable {
	public int FileCount { get; init; }
	public int DirectoryCount { get; init; }
	public long FirstFileOffset { get; init; }
	public long NextTableOffset { get; init; }
	public int FirstFileIndex { get; init; }
	public int LastFileIndex { get; init; }
	public long FirstAttributeOffset { get; init; }
	public long FirstSubdirectoryOffset { get; init; }
}
