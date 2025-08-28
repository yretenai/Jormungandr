using Jormungandr.IO;
using Jormungandr.Scimitar;

namespace Jormungandr.Headless;

internal class Program {
	private static void Main(string[] args) {
		var acExe = Directory.EnumerateFiles(args[0], "AC*.exe").FirstOrDefault() ?? string.Empty;

		var game = Path.GetFileNameWithoutExtension(acExe) switch {
			"ACValhalla" => ScimitarGame.ACK,
			"ACMirage" => ScimitarGame.ACRIFT,
			"ACShadows" => ScimitarGame.ACRED,
			_ => ScimitarGame.Unknown,
		};

		foreach (var file in Directory.EnumerateFiles(args[0], "*.forge")) {
			using var anvil = new AnvilFile(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
			foreach (var uid in anvil) {
				using var bundle = anvil.Open(uid);
				var span = bundle.Headers.Span;
				for (var i = 0; i < span.Length; ++i) {
					var assetUid = span[i];
					var obj = bundle.IsDataStream ? new TaggedPropertyFile(bundle, bundle.Assets[i], assetUid.ObjectId, game) : default;
					if (obj == null) {
						continue;
					}

					if (obj.Header is { ObjectNameIsEncrypted: true, ObjectName.Length: > 0 }) {
						continue;
					}

					Console.WriteLine($"{uid.Value:x16} {obj.Header.ObjectName}");
				}
			}
		}
	}
}
