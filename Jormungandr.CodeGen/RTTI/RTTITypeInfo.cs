namespace Jormungandr.CodeGen.RTTI;

internal record struct RTTITypeInfo {
	public uint TypeHash { get; set; }
	public int ArraySize { get; set; }
	public RTTIUbiType TypeId { get; set; }
	public RTTIUbiType InnerTypeId => (RTTIUbiType) BitSize;
	public uint BitSize { get; set; }
	public uint BitFieldSize { get; set; }
	public bool IsPrimitive { get; set; }
	public bool IsBitField { get; set; }

	public string GetTypeDescriptor(RTTIBlob rtti) =>
		TypeId switch {
			RTTIUbiType.StaticArray => (this with {
					TypeId = InnerTypeId,
				}).GetTypeDescriptor(rtti) + $"[{ArraySize}]",
			RTTIUbiType.BigArray or RTTIUbiType.SmallArray => (this with {
				TypeId = InnerTypeId,
			}).GetTypeDescriptor(rtti) + "[]",
			RTTIUbiType.Enum => rtti.GetName(TypeHash, "Enum"),
			RTTIUbiType.DataDrivenEnum => rtti.GetName(TypeHash, "DataDrivenEnum"),
			RTTIUbiType.Handle or RTTIUbiType.SloppyHandle => "Handle<" + rtti.GetName(TypeHash, "BaseClass") + ">",
			RTTIUbiType.Reference => "Reference<" + rtti.GetName(TypeHash, "BaseClass") + ">",
			RTTIUbiType.Object or RTTIUbiType.BaseObject => rtti.GetName(TypeHash, "BaseClass"),
			RTTIUbiType.ObjectPtr or RTTIUbiType.BaseObjectPtr => rtti.GetName(TypeHash, "BaseClass") + "*",
			RTTIUbiType.Bool => "bool",
			RTTIUbiType.Char => "char",
			RTTIUbiType.UInt8 => "byte",
			RTTIUbiType.Int8 => "sbyte",
			RTTIUbiType.UInt16 => "ushort",
			RTTIUbiType.Int16 => "short",
			RTTIUbiType.UInt32 => "uint",
			RTTIUbiType.Int32 => "int",
			RTTIUbiType.UInt64 => "long",
			RTTIUbiType.Int64 => "ulong",
			RTTIUbiType.Float => "float",
			RTTIUbiType.Vector2 => "Vector2",
			RTTIUbiType.Vector3 => "Vector3",
			RTTIUbiType.Vector4 => "Vector4",
			RTTIUbiType.Quaternion => "Quaternion",
			RTTIUbiType.Matrix3 => "Matrix3",
			RTTIUbiType.Matrix4 => "Matrix4",
			RTTIUbiType.ObjectId => "ObjectId",
			RTTIUbiType.String or RTTIUbiType.WideString => "string",
			_ => "UNKNOWN",
		};
}
