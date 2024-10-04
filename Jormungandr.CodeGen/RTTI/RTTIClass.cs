using System.Diagnostics;

namespace Jormungandr.CodeGen.RTTI;

public record RTTIClass {
	public List<RTTIClassField> Fields { get; set; } = [];
	public List<RTTIClassMethod> Methods { get; set; } = [];
	public uint ParentHash { get; set; }
	public uint ClassHash { get; set; }
	public string? Name { get; set; }
	public int Size { get; set; }
	public uint Flags { get; set; }
	public uint FieldFlags { get; set; }

	public uint Field24 { get; set; }
	public uint Field2c { get; set; }
	public uint Field30 { get; set; }
	public uint Field34 { get; set; }
	public uint Field38 { get; set; }
	public uint Field3c { get; set; }
	public uint Field40 { get; set; }
	public uint Field44 { get; set; }
	public uint Field46 { get; set; }
	public uint Field40old { get; set; }
	public uint Field44old { get; set; }

	public void Dump(RTTIBlob rtti, string path) {
		var name = rtti.GetName(ClassHash, "BaseObject");
		Console.WriteLine($"Wrote {name}");
		if (!name.All(x => char.IsAsciiLetterOrDigit(x) || x == '_')) {
			throw new UnreachableException();
		}

		var parentName = rtti.GetName(ParentHash, "BaseObject");

		using var writer = new StreamWriter(new FileStream(Path.Combine(path, name + ".txt"), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite));
		writer.Write($"// {ClassHash:x8}");
		writer.Write($"class {name} ");
		if (ParentHash > 1) {
			writer.Write($": {parentName} ");
		}

		writer.Write("{");

		if (Fields.Count == 0 && Methods.Count == 0) {
			writer.WriteLine("}");
			return;
		}

		writer.WriteLine();

		foreach (var field in Fields) {
			writer.Write("\t");
			writer.Write(field.GetFieldType(rtti));
			writer.Write($" {rtti.GetName(field.NameHash, "INVALID")}; // 0x{field.Offset:x4}");
			if (field.IsBitField) {
				writer.Write($", Bit Type: {RTTIClassField.GetTypeDescriptor(field.SecondaryType, RTTIUbiType.Unknown, field.ArraySize, field.TypeHash, rtti)}, Bit Mask: {field.BitMask:b14}, BitSize: {field.BitSize}, SizeMask: {field.SizeFlags:x8}");
			}

			writer.WriteLine();
		}

		foreach (var method in Methods) {
			writer.Write("\t");
			var firstRetVal = method.Arguments.FirstOrDefault(x => (x.Flags & 1) == 1);
			writer.Write(firstRetVal?.GetFieldType(rtti) ?? "void");
			writer.Write($" {method.Name}(");
			for (var index = 0; index < method.Arguments.Count; index++) {
				var arg = method.Arguments[index];
				if ((arg.Flags & 0x2) == 2) {
					writer.Write("ref ");
				}

				if ((arg.Flags & 0x1) == 1) {
					writer.Write("out ");
				}

				if (arg.Flags is not (0 or 1 or 2)) {
					Debugger.Break();
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
