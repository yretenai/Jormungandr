using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jormungandr.IO.Buffers;
using Jormungandr.IO.Structures;
using Jormungandr.Scimitar;
using Watersports.Compression;

namespace Jormungandr.IO;

public sealed class ForgeBundle : IDisposable {
	public ForgeBundle(ForgeFile forgeFile, ForgeFileEntry entry) {
		File = forgeFile;
		UId = entry.Id;

		var buffer = new PooledMemory<byte>(entry.Size);
		var span = buffer.Span;
		File.BaseStream.Position = entry.Offset;
		File.BaseStream.ReadExactly(span);
		if (MemoryMarshal.Read<uint>(span) >> 8 != ForgeBundleIdentifier.MAGIC) {
			Headers = new PooledMemory<ForgeBundleEntry>(1);
			Headers.Span[0] = new ForgeBundleEntry {
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
				Headers = RentedMemory<ForgeBundleEntry>.Empty;
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
			Headers = new PooledMemory<ForgeBundleEntry>(entryCount);
			var headerSpan = HeaderStream.Span;
			for (var i = 0; i < entryCount; ++i) {
				var header = MemoryMarshal.Read<ForgeBundleEntry>(headerSpan[headerOffset..]);
				Headers.Span[i] = header;
				headerOffset += Unsafe.SizeOf<ForgeBundleEntry>();
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

	public ForgeBundle(ForgeFile forgeFile, ObjectId uid) {
		File = forgeFile;
		UId = uid;
		DataStream = RentedMemory<byte>.Empty;
		Headers = RentedMemory<ForgeBundleEntry>.Empty;
	}

	public ForgeFile File { get; }
	public ObjectId UId { get; }

	public RentedMemory<ForgeBundleEntry> Headers { get; }
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

		var header = MemoryMarshal.Read<ForgeBundleHeader>(span);
		if (header.Identifier.Magic != ForgeBundleIdentifier.MAGIC) {
			return RentedMemory<byte>.Empty;
		}

		if (header.Identifier.ContainerType != ForgeBundleIdentifier.CONTAINER_MAGIC) {
			return RentedMemory<byte>.Empty;
		}

		readBytes += Unsafe.SizeOf<ForgeBundleHeader>();

		Debug.Assert(header.CompressionType is >= ForgeCompressionType.OodleKraken and <= ForgeCompressionType.OodleSelkieOpt);

		if (header.BlockCount == 0) {
			return RentedMemory<byte>.Empty;
		}

		var blocks = MemoryMarshal.Cast<byte, ForgeBundleBlock>(span[readBytes..])[..header.BlockCount];
		readBytes += Unsafe.SizeOf<ForgeBundleBlock>() * header.BlockCount;

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

			var compressionType = block.IsUncompressed ? ForgeCompressionType.None : header.CompressionType;
			var helperCompressionType = compressionType switch {
				                            ForgeCompressionType.Lzo1x => CompressionType.LZO1,
				                            ForgeCompressionType.Lzo1xOpt => CompressionType.LZO1,
				                            ForgeCompressionType.Lzo2a => CompressionType.LZO2,
				                            ForgeCompressionType.LZ4 => CompressionType.LZ4,
				                            ForgeCompressionType.LZ4HC => CompressionType.LZ4,
				                            ForgeCompressionType.OodleKraken => CompressionType.Oodle,
				                            ForgeCompressionType.OodleKrakenOpt => CompressionType.Oodle,
				                            ForgeCompressionType.OodleMermaid => CompressionType.Oodle,
				                            ForgeCompressionType.OodleMermaidOpt => CompressionType.Oodle,
				                            ForgeCompressionType.OodleSelkie => CompressionType.Oodle,
				                            ForgeCompressionType.OodleSelkieOpt => CompressionType.Oodle,
				                            ForgeCompressionType.None => CompressionType.None,
				                            ForgeCompressionType.Zlib => CompressionType.Zlib,
				                            ForgeCompressionType.ZStandard => CompressionType.Zstd,
				                            ForgeCompressionType.ZStandardOpt => CompressionType.Zstd,
				                            _ => throw new NotSupportedException(),
			                            };

			CompressionHelper.Decompress(helperCompressionType, compressedBlock, targetBlock);
		}

		return data;
	}
}
