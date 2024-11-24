using System.Diagnostics;

namespace Jormungandr.CodeGen.RTTI;

internal record RTTIClass {
	public List<RTTIClassField> Fields { get; set; } = [];
	public List<RTTIClassMethod> Methods { get; set; } = [];
	public uint ParentHash { get; set; }
	public uint ClassHash { get; set; }
	public string? Name { get; set; }
	public int Size { get; set; }
	public uint Alignment { get; set; }
	public ulong Flags { get; set; }
	public uint FieldFlags { get; set; }
	public uint DynamicPropertiesOffset { get; set; }
	public ulong Signature { get; set; }
	public uint Index { get; set; }
	public int InheritanceMin { get; set; }
	public int InheritanceMax { get; set; }

	public void Dump(RTTIBlob rtti, string path) {
		var name = rtti.GetName(ClassHash, "BaseObject");
		Console.WriteLine($"Wrote {name}");
		if (!name.All(x => char.IsAsciiLetterOrDigit(x) || x == '_')) {
			throw new UnreachableException();
		}

		var parentName = rtti.GetName(ParentHash, "BaseObject");

		using var writer = new StreamWriter(new FileStream(Path.Combine(path, name + ".txt"), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite));
		writer.WriteLine($"// {ClassHash:x8}");
		writer.Write($"class {name} ");
		if (ParentHash > 1) {
			writer.Write($": {parentName} ");
		}

		writer.Write("{");

		if (Fields.Count == 0 && Methods.Count == 0) {
			writer.Write("}");
		}

		writer.WriteLine($"// Flags: {Flags:b16}, Index: 0x{Index:x8}, Signature: 0x{Signature:x16}, Dynamic Properties: 0x{DynamicPropertiesOffset:x}, Size: 0x{Size:x8}, Alignment: 0x{Alignment:x8}");

		if (Fields.Count == 0 && Methods.Count == 0) {
			return;
		}

		foreach (var field in Fields) {
			writer.Write("\t");
			writer.Write(field.TypeInfo.GetTypeDescriptor(rtti));
			writer.Write($" {rtti.GetName(field.NameHash, "INVALID")}; // 0x{field.AccessInfo.Offset:x4} Type Id: {field.AccessInfo.TypeId}");
			if (field.TypeInfo.IsBitField) {
				writer.Write($", Bit Type: {field.TypeInfo.GetTypeDescriptor(rtti)}, Bit Offset: {field.AccessInfo.BitOffset}, Bit Size: {field.TypeInfo.BitSize}");
			}

			writer.WriteLine();
		}

		foreach (var method in Methods) {
			writer.Write("\t");
			var firstRetVal = method.Arguments.FirstOrDefault(x => x.Direction == 1);
			writer.Write(firstRetVal?.GetFieldType(rtti) ?? "void");
			writer.Write($" {method.Name}(");
			for (var index = 0; index < method.Arguments.Count; index++) {
				var arg = method.Arguments[index];
				switch (arg.Direction) {
					case 1:
						writer.Write("out ");
						break;
					case 2:
						writer.Write("ref ");
						break;
				}

				writer.Write(arg.GetFieldType(rtti) + " " + arg.Name);

				if (index < method.Arguments.Count - 1) {
					writer.Write(", ");
				}
			}

			writer.WriteLine(");");
		}

		writer.WriteLine("}");
	}
}
