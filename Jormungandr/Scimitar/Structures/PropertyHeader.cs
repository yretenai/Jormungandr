using Jormungandr.IO;

namespace Jormungandr.Scimitar.Structures;

public record PropertyHeader {
	public PropertyHeader(MemoryReader reader) {
		Tag = reader.Read<uint>();
		Size = reader.Read<int>();
		ObjectName = reader.ReadString(out var objectNameIsEncrypted);
		ObjectNameIsEncrypted = objectNameIsEncrypted;
		UId = reader.Read<ObjectId>();
		Unknown = reader.ReadByte(); // zero?
	}

	public bool ObjectNameIsEncrypted { get; }
	public int Size { get; }
	public uint Tag { get; }
	public string ObjectName { get; }
	public ObjectId UId { get; }
	public byte Unknown { get; }
}
