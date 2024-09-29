namespace Jormungandr.CodeGen.RTTI;

public record RTTIClassMethodArgument {
	public uint TypeHash { get; set; }
	public int ArraySize { get; set; }
	public uint TypeFlags { get; set; }
	public uint NameHash { get; set; }
	public string? Name { get; set; }
	public uint Flags { get; set; }

	public uint FieldC { get; set; }
	public uint Field1c { get; set; }

	public RTTIUbiType PrimaryType => (RTTIUbiType) (TypeFlags & 0x3f);
	public RTTIUbiType SecondaryType => (RTTIUbiType) ((TypeFlags >> 7) & 0x3f);

	public string GetFieldType(RTTIBlob rtti) => RTTIClassField.GetTypeDescriptor(PrimaryType, SecondaryType, ArraySize, TypeHash, rtti);
}
