using Jormungandr.IO;

namespace Jormungandr.Headless;

internal class Program {
	private static void Main(string[] args) {
		foreach (var file in Directory.EnumerateFiles(args[0], "*.forge")) {
			using var forge = new ForgeFile(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
			foreach (var uid in forge) {
				using var bundle = forge.Open(uid);
				Console.WriteLine(uid);
			}
		}
	}
}
