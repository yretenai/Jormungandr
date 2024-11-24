using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jormungandr.CodeGen.RTTI;

internal record RTTIBlob {
	public Dictionary<uint, RTTIClass> Classes { get; set; } = [];
	public Dictionary<uint, RTTIEnum> Enums { get; set; } = [];
	public HashSet<string> Names { get; set; } = [];
	public Dictionary<string, string> Build { get; set; } = [];

	private Dictionary<uint, string> HashTable { get; } = [];
	internal HashSet<uint> MissingHashes { get; } = [];

	private static JsonSerializerOptions Options { get; } = new() {
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};

	public string GetName(uint hash, string fallback) {
		if (hash is 0 or 1) {
			return fallback;
		}

		if (HashTable.TryGetValue(hash, out var name)) {
			return name;
		}

		MissingHashes.Add(hash);
		return $"x{hash:x8}";
	}

	public static RTTIBlob Create(string rttiPath, string namesPath) {
		using var stream = new FileStream(rttiPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		var rttiBlob = JsonSerializer.Deserialize<RTTIBlob>(stream, Options) ?? throw new UnreachableException();
		if (File.Exists(namesPath)) {
			using var names = new StreamReader(new FileStream(namesPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
			while (names.ReadLine() is { } name) {
				rttiBlob.Names.Add(name);
			}
		}

		using var crc = new CRC(0x04C11DB7, uint.MaxValue, uint.MaxValue, true, true);
		if (crc.ComputeHash("123456789") != 0xCBF43926) {
			throw new UnreachableException();
		}

		crc.Reset();

		foreach (var rawName in rttiBlob.CollectNames()) {
			var name = rawName.Replace(':', '_');
			if (name.StartsWith("m_")) {
				name = name[2..];
			}

			rttiBlob.HashTable[crc.ComputeHash(rawName)] = name;
			crc.Reset();
		}

		return rttiBlob;
	}

	public IEnumerable<string> CollectNames() {
		foreach (var name in Names) {
			yield return name;
		}

		foreach (var (_, cls) in Classes) {
			if (!string.IsNullOrEmpty(cls.Name)) {
				yield return cls.Name;
			}

			foreach (var method in cls.Methods) {
				if (!string.IsNullOrEmpty(method.Name)) {
					yield return method.Name;
				}

				foreach (var argument in method.Arguments) {
					if (!string.IsNullOrEmpty(argument.Name)) {
						yield return argument.Name;
					}
				}
			}
		}
	}
}
