using System.Runtime.InteropServices;

namespace Jormungandr.IO.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct AnvilBundleIdentifier {
	public const uint CONTAINER_MAGIC = 0x1004FA99;
	public const uint MAGIC = 0x57FBAA;

	public ulong FullMagic { get; init; }
	public uint ContainerType => (uint) (FullMagic >> 32);
	public byte Version => (byte) (FullMagic & 0xFF); // valid: 1 through 0x36
	public uint Magic => (uint) (GlobalMagic & 0xFFFFFF);
	public ulong GlobalMagic => FullMagic >> 8;
}
