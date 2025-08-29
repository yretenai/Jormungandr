using System.Runtime.InteropServices;

namespace Jormungandr.Scimitar;

[StructLayout(LayoutKind.Sequential, Size = 8)]
public readonly record struct ObjectId(ulong Value);
