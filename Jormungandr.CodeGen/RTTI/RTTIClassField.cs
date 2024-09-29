namespace Jormungandr.CodeGen.RTTI;

public record RTTIClassField {
	public uint Flags { get; set; }
	public uint NameHash { get; set; }
	public uint TypeHash { get; set; }
	public int ArraySize { get; set; }
	public uint TypeFlags { get; set; }
	public uint SizeFlags { get; set; }
	public uint Field14 { get; set; }
	public uint Field16 { get; set; }

	public RTTIUbiType PrimaryType => (RTTIUbiType) (TypeFlags & 0x3f);
	public RTTIUbiType SecondaryType => (RTTIUbiType) ((TypeFlags >> 7) & 0x3f);
	public uint Offset => SizeFlags >> 18;
	public uint BitSize => (SizeFlags & 0xf) + 1;
	public uint BitMask => (SizeFlags >> 4) & 0x3fff;
	public bool IsBitField => TypeFlags >> 14 == 1;

	public static string GetTypeDescriptor(RTTIUbiType primary, RTTIUbiType secondary, int size, uint hash, RTTIBlob rtti) =>
		primary switch {
			RTTIUbiType.StaticArray => GetTypeDescriptor(secondary, RTTIUbiType.Unknown, size, hash, rtti),
			RTTIUbiType.BigArray or RTTIUbiType.SmallArray => GetTypeDescriptor(secondary, RTTIUbiType.Unknown, size, hash, rtti) + "[]",
			RTTIUbiType.Enum => rtti.GetName(hash, "Enum"),
			RTTIUbiType.DataDrivenEnum => rtti.GetName(hash, "DataDrivenEnum"),
			RTTIUbiType.Handle or RTTIUbiType.SloppyHandle => "Handle<" + rtti.GetName(hash, "BaseClass") + ">",
			RTTIUbiType.Reference => "Reference<" + rtti.GetName(hash, "BaseClass") + ">",
			RTTIUbiType.Object or RTTIUbiType.BaseObject => rtti.GetName(hash, "BaseClass"),
			RTTIUbiType.ObjectPtr or RTTIUbiType.BaseObjectPtr => rtti.GetName(hash, "BaseClass") + "*",
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

	public string GetFieldType(RTTIBlob rtti) => GetTypeDescriptor(PrimaryType, SecondaryType, ArraySize, TypeHash, rtti);
}
