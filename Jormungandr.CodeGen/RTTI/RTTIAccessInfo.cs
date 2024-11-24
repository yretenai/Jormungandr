namespace Jormungandr.CodeGen.RTTI;

internal record struct RTTIAccessInfo {
	public uint TypeId { get; set; }
	public uint BitOffset { get; set; }
	public uint BitSize { get; set; }
	public bool IsBitField { get; set; }
	public bool IsDynamic { get; set; }
	public uint Offset { get; set; }
}
