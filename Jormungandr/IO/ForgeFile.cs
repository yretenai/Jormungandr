using System.Collections;
using System.Diagnostics;
using Jormungandr.IO.Structures;
using Jormungandr.Scimitar;

namespace Jormungandr.IO;

public sealed class ForgeFile : IDisposable, IEnumerable<ObjectId> {
	public ForgeFile(Stream stream) {
		BaseStream = stream;

		Header = BaseStream.ReadExactly<ForgeHeader>();
		if (Header.Magic != "scimitar"u8) {
			throw new InvalidDataException("Not a scimitar file");
		}

		BaseStream.Position = Header.FileAllocationTableOffset;
		FileTableHeader = BaseStream.ReadExactly<ForgeCentralFileTable>();

		Debug.Assert(FileTableHeader.FileTableCount == 1);
		FileTables = new PooledMemory<ForgeFileTable>(FileTableHeader.FileTableCount);
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
			using var entries = new PooledMemory<ForgeFileEntry>(fileTable.FileCount);
			BaseStream.ReadExactly(entries.Span);
			foreach (var entry in entries) {
				FileEntries.Add(entry.Id, entry);
			}
		}
	}

	public Stream BaseStream { get; }
	public ForgeHeader Header { get; }
	public ForgeCentralFileTable FileTableHeader { get; }
	public RentedMemory<ForgeFileTable> FileTables { get; }
	public Dictionary<ObjectId, ForgeFileEntry> FileEntries { get; } = [];

	public void Dispose() {
		BaseStream.Dispose();
		FileTables.Dispose();
	}

	public IEnumerator<ObjectId> GetEnumerator() => FileEntries.Keys.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public ForgeBundle Open(ObjectId uid) =>
		!FileEntries.TryGetValue(uid, out var entry)
			? new ForgeBundle(this, uid)
			: new ForgeBundle(this, entry);
}
