namespace Jormungandr.CodeGen.RTTI;

public record RTTIClassMethod {
	public List<RTTIClassMethodArgument> Arguments { get; set; } = [];
	public uint NameHash { get; set; }
	public string? Name { get; set; }
	public uint Flags { get; set; }

	public uint Field12 { get; set; }
	public uint Field1c { get; set; }
}
