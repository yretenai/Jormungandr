using Jormungandr.Cryptography;
using Jormungandr.IO;
using Jormungandr.IO.Buffers;
using Jormungandr.Scimitar.Structures;

namespace Jormungandr.Scimitar;

public class TaggedPropertyFile {
	public TaggedPropertyFile(AnvilBundle bundle, RentedMemory<byte> buffer, ObjectId uid, ScimitarGame game) {
		Bundle = bundle;
		Buffer = buffer;
		UId = uid;

		var keyRing = game switch {
			ScimitarGame.ACK => KeyRing.ACK,
			ScimitarGame.ACRIFT => KeyRing.ACRIFT,
			_ => null,
		};

		using var reader = new MemoryReader(buffer, false);
		Header = new PropertyHeader(UId, reader, keyRing);
		// todo: load properties.
	}

	public PropertyHeader Header { get; }
	public AnvilBundle Bundle { get; }
	public RentedMemory<byte> Buffer { get; }
	public ObjectId UId { get; }
}
