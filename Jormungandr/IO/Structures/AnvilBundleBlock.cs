using System.Runtime.InteropServices;

namespace Jormungandr.IO.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct AnvilBundleBlock {
	public int UncompressedSize { get; init; }
	public int CompressedSize { get; init; }
	public bool IsUncompressed => UncompressedSize == CompressedSize;
}
