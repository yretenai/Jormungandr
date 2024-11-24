namespace Jormungandr.CodeGen.RTTI;

internal record RTTIEnumValue {
	public uint Value { get; set; }
	public uint NameHash { get; set; }
}
