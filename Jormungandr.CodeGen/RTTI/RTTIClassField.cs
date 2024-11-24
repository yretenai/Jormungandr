namespace Jormungandr.CodeGen.RTTI;

internal record RTTIClassField {
	public uint Flags { get; set; }
	public uint NameHash { get; set; }
	public RTTITypeInfo TypeInfo { get; set; } = new();
	public RTTIAccessInfo AccessInfo { get; set; } = new();
}
