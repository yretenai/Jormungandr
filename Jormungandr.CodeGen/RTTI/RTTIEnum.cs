namespace Jormungandr.CodeGen.RTTI;

public record RTTIEnum {
	public List<RTTIEnumValue> Values { get; set; } = [];
	public uint NameHash { get; set; }

	public void Dump(RTTIBlob rtti, string path) {
	}
}
