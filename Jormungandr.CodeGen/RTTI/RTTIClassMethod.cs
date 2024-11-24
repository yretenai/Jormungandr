namespace Jormungandr.CodeGen.RTTI;

internal record RTTIClassMethod {
	public List<RTTIClassMethodArgument> Arguments { get; set; } = [];
	public uint NameHash { get; set; }
	public string? Name { get; set; }
	public uint Flags { get; set; }
}
