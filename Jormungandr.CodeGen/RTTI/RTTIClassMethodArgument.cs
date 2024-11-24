namespace Jormungandr.CodeGen.RTTI;

internal record RTTIClassMethodArgument {
	public RTTITypeInfo TypeInfo { get; set; } = new();
	public uint NameHash { get; set; }
	public string? Name { get; set; }
	public uint Direction { get; set; }

	public string GetFieldType(RTTIBlob rtti) => TypeInfo.GetTypeDescriptor(rtti);
}
