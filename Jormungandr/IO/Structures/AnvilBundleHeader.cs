using System.Runtime.InteropServices;

namespace Jormungandr.IO.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct AnvilBundleHeader {
	public AnvilBundleIdentifier Identifier { get; init; }
	public ushort Version { get; init; }
	public AnvilCompressionType CompressionType { get; init; }
	public AnvilBundleFlags Flags { get; init; }
	public int BlockCount { get; init; }
}
