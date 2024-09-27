using System.Diagnostics;
using Jormungandr.IO;
using Jormungandr.IO.Buffers;
using Jormungandr.Scimitar.Structures;

namespace Jormungandr.Scimitar;

public class TaggedPropertyFile {
	public TaggedPropertyFile(ForgeBundle bundle, RentedMemory<byte> buffer, ObjectId uid) {
		Bundle = bundle;
		Buffer = buffer;
		UId = uid;

		using var reader = new MemoryReader(buffer, false);
		Header = new PropertyHeader(UId, reader);
		if (Header.UId != default) {
			Debug.Assert(Header.UId == UId);
		}
		// todo: load properties.
	}

	public PropertyHeader Header { get; }
	public ForgeBundle Bundle { get; }
	public RentedMemory<byte> Buffer { get; }
	public ObjectId UId { get; }
}
