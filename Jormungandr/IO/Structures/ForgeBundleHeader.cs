using System.Runtime.InteropServices;

namespace Jormungandr.IO.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct ForgeBundleHeader {
	public ForgeBundleIdentifier Identifier { get; init; }
	public ushort Version { get; init; }
	public ForgeCompressionType CompressionType { get; init; }
	public ForgeBundleFlags Flags { get; init; }
	public int BlockCount { get; init; }
}
