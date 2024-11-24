using System.Reflection;
using Jormungandr.CodeGen.RTTI;

namespace Jormungandr.CodeGen;

internal static class Program {
	private static int Main(string[] args) {
		Console.WriteLine($"Jormungandr CodeGen v{Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0"} (Jormywormy)");

		var mode = "help";
		var rttiPath = string.Empty;
		var namesPath = string.Empty;
		var outputPath = string.Empty;
		if (args.Length >= 4) {
			mode = args[0].ToLowerInvariant();
			rttiPath = args[1];
			namesPath = args[2];
			outputPath = args[3];
		}

		switch (mode) {
			case "-h" or "--help" or "help":
				return PrintHelp();
			case "-v" or "--version" or "version":
				return 0;
			default:
				switch (mode) {
					case "generate":
						// todo
						break;
					case "dump": {
						var rtti = RTTIBlob.Create(rttiPath, namesPath);
						Directory.CreateDirectory(Path.Combine(outputPath, "Classes"));
						foreach (var (_, cls) in rtti.Classes) {
							cls.Dump(rtti, Path.Combine(outputPath, "Classes"));
						}

						Directory.CreateDirectory(Path.Combine(outputPath, "Enums"));
						foreach (var (_, enm) in rtti.Enums) {
							enm.Dump(rtti, Path.Combine(outputPath, "Enums"));
						}

						break;
					}
					case "names": {
						var rtti = RTTIBlob.Create(rttiPath, namesPath);
						using var output = new StreamWriter(new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite));
						foreach (var name in rtti.CollectNames()) {
							output.WriteLine(name);
						}

						break;
					}
					default:
						return PrintHelp();
				}

				return 0;
		}
	}

	private static int PrintHelp() {
		Console.Error.WriteLine("Usage: Jormungandr.CodeGen <mode> /path/to/rtti.json /path/to/names.list /path/to/output");
		Console.Error.WriteLine("Available Modes:");
		Console.Error.WriteLine("\thelp - print this help text");
		Console.Error.WriteLine("\tversion - print version and exit");
		Console.Error.WriteLine("\tgenerate - generate C# classes");
		Console.Error.WriteLine("\tdump - dump human readable classes");
		Console.Error.WriteLine("\tnames - generate file list from existing RTTI jsons");
		return 1;
	}
}
