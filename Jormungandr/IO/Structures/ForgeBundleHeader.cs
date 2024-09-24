using System.Runtime.InteropServices;

namespace Jormungandr.IO.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct ForgeBundleHeader {
	public uint Magic { get; init; }
	public uint ContainerMagic { get; init; }
	public ushort Version { get; init; }
	public ForgeCompressionType CompressionType { get; init; } // note: is this valid? conflicting prior art implementations.
	public uint Flags { get; init; }
	public int BlockCount { get; init; }
}
