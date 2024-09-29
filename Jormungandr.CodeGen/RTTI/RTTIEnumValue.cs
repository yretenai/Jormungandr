namespace Jormungandr.CodeGen.RTTI;

public record RTTIEnumValue {
	public uint Value { get; set; }
	public uint NameHash { get; set; }
}
