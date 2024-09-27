using Jormungandr.IO;
using Jormungandr.Scimitar;

namespace Jormungandr.Headless;

internal class Program {
	private static void Main(string[] args) {
		foreach (var file in Directory.EnumerateFiles(args[0], "*.forge")) {
			using var forge = new ForgeFile(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
			var outputPath = default(string);
			if (args.Length > 1) {
				outputPath = Path.Combine(args[1], Path.GetFileNameWithoutExtension(file));
				Directory.CreateDirectory(outputPath);
			}

			foreach (var uid in forge) {
				using var bundle = forge.Open(uid);
				var span = bundle.Headers.Span;
				for (var i = 0; i < span.Length; ++i) {
					var assetUid = span[i];
					var obj = bundle.IsDataStream ? new TaggedPropertyFile(bundle, bundle.Assets[i], assetUid.ObjectId) : default;
					var name = assetUid.ObjectId.ToString();
					Console.WriteLine(name);
					if (!string.IsNullOrEmpty(outputPath)) {
						if (obj?.Header.ObjectName.Length > 0) {
							name += "_" + obj.Header.ObjectName[..Math.Min(128, obj.Header.ObjectName.Length)];
						}

						using var buffer = new FileStream(Path.Combine(outputPath, name + "." + (obj?.Header.Tag.ToString("x8") ?? "bin")), FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
						buffer.Write(bundle.Assets[i].Span);
					}
				}
			}
		}
	}
}
