using System.Collections;
using System.Diagnostics;
using Jormungandr.IO.Buffers;
using Jormungandr.IO.Structures;
using Jormungandr.Scimitar;

namespace Jormungandr.IO;

public sealed class AnvilFile : IDisposable, IEnumerable<ObjectId> {
	public AnvilFile(Stream stream) {
		BaseStream = stream;

		Header = BaseStream.ReadExactly<AnvilHeader>();
		if (Header.Magic != "scimitar"u8) {
			throw new InvalidDataException("Not a scimitar file");
		}

		Debug.Assert(Header.Version == 29);

		BaseStream.Position = Header.FileAllocationTableOffset;
		FileTableHeader = BaseStream.ReadExactly<AnvilCentralFileTable>();

		Debug.Assert(FileTableHeader.FileTableCount == 1);
		FileTables = new PooledMemory<AnvilFileTable>(FileTableHeader.FileTableCount);
		BaseStream.Position = FileTableHeader.FileTableOffset;
		BaseStream.ReadExactly(FileTables.Span);

		FileEntries.EnsureCapacity(FileTableHeader.FileTableCount);
		foreach (var fileTable in FileTables) {
			Debug.Assert(fileTable.FirstAttributeOffset == -1);
			Debug.Assert(fileTable.FirstSubdirectoryOffset == -1);

			if (fileTable.FileCount == 0 || fileTable.FirstFileOffset == -1) {
				continue;
			}

			BaseStream.Position = fileTable.FirstFileOffset;
			using var entries = new PooledMemory<AnvilFileEntry>(fileTable.FileCount);
			BaseStream.ReadExactly(entries.Span);
			foreach (var entry in entries) {
				FileEntries.Add(entry.Id, entry);
			}
		}
	}

	public Stream BaseStream { get; }
	public AnvilHeader Header { get; }
	public AnvilCentralFileTable FileTableHeader { get; }
	public RentedMemory<AnvilFileTable> FileTables { get; }
	public Dictionary<ObjectId, AnvilFileEntry> FileEntries { get; } = [];

	public void Dispose() {
		BaseStream.Dispose();
		FileTables.Dispose();
	}

	public IEnumerator<ObjectId> GetEnumerator() => FileEntries.Keys.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public AnvilBundle Open(ObjectId uid) =>
		FileEntries.TryGetValue(uid, out var entry)
			? new AnvilBundle(this, entry)
			: new AnvilBundle(this, uid);
}
