using System.Diagnostics;

namespace Jormungandr.CodeGen.RTTI;

public record RTTIEnum {
	public List<RTTIEnumValue> Values { get; set; } = [];
	public uint NameHash { get; set; }

	public void Dump(RTTIBlob rtti, string path) {
		var name = rtti.GetName(NameHash, "Enum");
		Console.WriteLine($"Wrote {name}");
		if (!name.All(x => char.IsAsciiLetterOrDigit(x) || x == '_')) {
			throw new UnreachableException();
		}

		using var writer = new StreamWriter(new FileStream(Path.Combine(path, name + ".txt"), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite));
		writer.Write($"// {NameHash:x8}");
		writer.Write($"enum {name} {{");

		if (Values.Count == 0) {
			writer.WriteLine("}");
			return;
		}

		writer.WriteLine();

		foreach (var value in Values) {
			var valueName = rtti.GetName(value.NameHash, "Enum");
			writer.WriteLine($"\t{valueName} = {value.Value},");
		}

		writer.WriteLine("}");
	}
}
