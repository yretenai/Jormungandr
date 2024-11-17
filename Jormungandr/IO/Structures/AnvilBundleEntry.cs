using System.Runtime.InteropServices;
using Jormungandr.Scimitar;

namespace Jormungandr.IO.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct AnvilBundleEntry {
	public ObjectId ObjectId { get; init; }
	public int Size { get; init; }
	public ushort DependencyCount { get; init; }
}
