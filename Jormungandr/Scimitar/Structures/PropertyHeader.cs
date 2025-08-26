using Jormungandr.Cryptography;
using Jormungandr.IO;

namespace Jormungandr.Scimitar.Structures;

public record PropertyHeader {
	public PropertyHeader(ObjectId uid, MemoryReader reader, KeyRing? keyRing) {
		Tag = reader.Read<uint>();
		Size = reader.Read<int>();
		ObjectName = reader.ReadString(Tag, uid, out var objectNameIsEncrypted, keyRing);
		ObjectNameIsEncrypted = objectNameIsEncrypted;

		var hasBlockInfo = reader.ReadFlag();
		if (hasBlockInfo) {
			BlockAllocator = new PropertyBlockAllocator(reader);
		}
	}

	public bool ObjectNameIsEncrypted { get; }
	public int Size { get; }
	public uint Tag { get; }
	public string ObjectName { get; }
	public PropertyBlockAllocator BlockAllocator { get; }
}
