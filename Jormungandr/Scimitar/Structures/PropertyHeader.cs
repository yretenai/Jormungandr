using Jormungandr.IO;

namespace Jormungandr.Scimitar.Structures;

public record PropertyHeader {
	public PropertyHeader(ObjectId uid, MemoryReader reader) {
		Tag = reader.Read<uint>();
		Size = reader.Read<int>();
		ObjectName = reader.ReadString(Tag, uid, out var objectNameIsEncrypted);
		ObjectNameIsEncrypted = objectNameIsEncrypted;

		var hasBlob = reader.ReadFlag();
		if (hasBlob) {
			reader.Position += 3;
			var size = reader.Read<int>() * 12;
			reader.Position -= 7;
			Blob = reader.ReadBytes(size + 7);
		}

		var hasUid = reader.ReadFlag();
		if (hasUid) {
			UId = reader.Read<ObjectId>();
		}
	}

	public bool ObjectNameIsEncrypted { get; }
	public int Size { get; }
	public uint Tag { get; }
	public string ObjectName { get; }
	public ObjectId UId { get; }
	public Memory<byte> Blob { get; }
}
