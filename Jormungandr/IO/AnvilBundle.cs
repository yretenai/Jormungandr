using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jormungandr.IO.Buffers;
using Jormungandr.IO.Structures;
using Jormungandr.Scimitar;
using Waterfall.Compression;

namespace Jormungandr.IO;

public sealed class AnvilBundle : IDisposable {
	public AnvilBundle(AnvilFile anvilFile, AnvilFileEntry entry) {
		File = anvilFile;
		UId = entry.Id;

		var buffer = new PooledMemory<byte>(entry.Size);
		var span = buffer.Span;
		File.BaseStream.Position = entry.Offset;
		File.BaseStream.ReadExactly(span);
		if (MemoryMarshal.Read<uint>(span) >> 8 != AnvilBundleIdentifier.MAGIC) {
			Headers = new PooledMemory<AnvilBundleEntry>(1);
			Headers.Span[0] = new AnvilBundleEntry {
				ObjectId = UId,
				Size = span.Length,
			};
			DataStream = buffer;
			Assets.Add(DataStream);
			return;
		}

		IsDataStream = true;

		try {
			HeaderStream = ReadBlock(buffer.Memory, out var dataOffset);
			if (dataOffset == 0) {
				throw new InvalidOperationException();
			}

			var entryCount = MemoryMarshal.Read<ushort>(HeaderStream.Span);
			if (dataOffset == entry.Size || entryCount == 0) {
				HeaderStream.Dispose();
				DataStream = RentedMemory<byte>.Empty;
				Headers = RentedMemory<AnvilBundleEntry>.Empty;
				return;
			}

			DataStream = ReadBlock(buffer.Memory[dataOffset..], out var endOffset);
			if (endOffset == 0) {
				throw new InvalidOperationException();
			}

			if (dataOffset + endOffset != entry.Size) {
				throw new InvalidDataException();
			}

			var offset = 0;
			var headerOffset = 2;
			Headers = new PooledMemory<AnvilBundleEntry>(entryCount);
			var headerSpan = HeaderStream.Span;
			for (var i = 0; i < entryCount; ++i) {
				var header = MemoryMarshal.Read<AnvilBundleEntry>(headerSpan[headerOffset..]);
				Headers.Span[i] = header;
				headerOffset += Unsafe.SizeOf<AnvilBundleEntry>();
				Assets.Add(new SloppyMemory<byte>(DataStream, offset, header.Size));
				offset += header.Size;
				if (header.DependencyCount > 0) {
					Dependencies.Add(new CastMemory<ushort, byte>(HeaderStream, headerOffset, header.DependencyCount << 1));
					headerOffset += header.DependencyCount << 1;
				} else {
					Dependencies.Add(RentedMemory<ushort>.Empty);
				}
			}
		} finally {
			buffer.Dispose();
		}
	}

	public AnvilBundle(AnvilFile anvilFile, ObjectId uid) {
		File = anvilFile;
		UId = uid;
		DataStream = RentedMemory<byte>.Empty;
		Headers = RentedMemory<AnvilBundleEntry>.Empty;
	}

	public AnvilFile File { get; }
	public ObjectId UId { get; }

	public RentedMemory<AnvilBundleEntry> Headers { get; }
	public List<RentedMemory<ushort>> Dependencies { get; } = [];
	public List<RentedMemory<byte>> Assets { get; } = [];
	private RentedMemory<byte> DataStream { get; }
	private RentedMemory<byte>? HeaderStream { get; }
	public bool IsDataStream { get; }

	public void Dispose() {
		foreach (var asset in Assets) {
			asset.Dispose();
		}

		Assets.Clear();
		Dependencies.Clear();
		Headers.Dispose();
		DataStream.Dispose();
		HeaderStream?.Dispose();
	}

	private static RentedMemory<byte> ReadBlock(Memory<byte> memory, out int readBytes) {
		var span = memory.Span;
		readBytes = 0;

		var header = MemoryMarshal.Read<AnvilBundleHeader>(span);
		if (header.Identifier.Magic != AnvilBundleIdentifier.MAGIC) {
			return RentedMemory<byte>.Empty;
		}

		if (header.Identifier.ContainerType != AnvilBundleIdentifier.CONTAINER_MAGIC) {
			return RentedMemory<byte>.Empty;
		}

		readBytes += Unsafe.SizeOf<AnvilBundleHeader>();

		Debug.Assert(header.CompressionType is >= AnvilCompressionType.OodleKraken and <= AnvilCompressionType.OodleSelkieOpt);

		if (header.BlockCount == 0) {
			return RentedMemory<byte>.Empty;
		}

		var blocks = MemoryMarshal.Cast<byte, AnvilBundleBlock>(span[readBytes..])[..header.BlockCount];
		readBytes += Unsafe.SizeOf<AnvilBundleBlock>() * header.BlockCount;

		// loop 1: get total size
		var size = 0;
		foreach (var block in blocks) {
			size += block.UncompressedSize;
		}

		// loop 2: decompress
		var data = new PooledMemory<byte>(size);

		var dataMemory = data.Memory;
		var offset = 0;
		foreach (var block in blocks) {
			_ = MemoryMarshal.Read<uint>(span); // checksum
			var compressedBlock = memory.Slice(readBytes + 4, block.CompressedSize);
			readBytes += 4 + block.CompressedSize;

			var targetBlock = dataMemory.Slice(offset, block.UncompressedSize);
			offset += block.UncompressedSize;

			var compressionType = block.IsUncompressed ? AnvilCompressionType.None : header.CompressionType;
			var helperCompressionType = compressionType switch {
				AnvilCompressionType.Lzo1x => CompressionType.LZO1,
				AnvilCompressionType.Lzo1xOpt => CompressionType.LZO1,
				AnvilCompressionType.Lzo2a => CompressionType.LZO2,
				AnvilCompressionType.LZ4 => CompressionType.LZ4,
				AnvilCompressionType.LZ4HC => CompressionType.LZ4,
				AnvilCompressionType.OodleKraken => CompressionType.Oodle,
				AnvilCompressionType.OodleKrakenOpt => CompressionType.Oodle,
				AnvilCompressionType.OodleMermaid => CompressionType.Oodle,
				AnvilCompressionType.OodleMermaidOpt => CompressionType.Oodle,
				AnvilCompressionType.OodleSelkie => CompressionType.Oodle,
				AnvilCompressionType.OodleSelkieOpt => CompressionType.Oodle,
				AnvilCompressionType.None => CompressionType.None,
				AnvilCompressionType.Zlib => CompressionType.Zlib,
				AnvilCompressionType.ZStandard => CompressionType.Zstd,
				AnvilCompressionType.ZStandardOpt => CompressionType.Zstd,
				_ => throw new NotSupportedException(),
			};

			CompressionHelper.Decompress(helperCompressionType, compressedBlock, targetBlock);
		}

		return data;
	}
}
