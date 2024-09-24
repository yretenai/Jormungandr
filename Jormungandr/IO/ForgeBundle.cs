using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jormungandr.IO.Structures;
using Jormungandr.Scimitar;
using Watersports.Compression;

namespace Jormungandr.IO;

public sealed class ForgeBundle : IDisposable {
	private static int MinimumBlockSize { get; } = Unsafe.SizeOf<ForgeBundleHeader>() + Unsafe.SizeOf<ForgeBundleBlock>() + sizeof(uint);

	public ForgeBundle(ForgeFile forgeFile, ForgeFileEntry entry) {
		File = forgeFile;
		UId = entry.Id;

		using var buffer = new PooledMemory<byte>(entry.Size);
		var memory = buffer.Memory;
		var span = buffer.Span;
		File.BaseStream.Position = entry.Offset;
		File.BaseStream.ReadExactly(span);

		while (span.Length >= MinimumBlockSize) {
			var header = MemoryMarshal.Read<ForgeBundleHeader>(span);
			span = span[Unsafe.SizeOf<ForgeBundleHeader>()..];
			if (header.BlockCount == 0) {
				Entries.Add([]);
				continue;
			}

			var blocks = MemoryMarshal.Cast<byte, ForgeBundleBlock>(span)[..header.BlockCount];
			span = span[(Unsafe.SizeOf<ForgeBundleBlock>() * header.BlockCount)..];

			// loop 1: get total size
			var size = 0;
			foreach (var block in blocks) {
				size += block.UncompressedSize;
			}

			// loop 2: decompress
			var data = new PooledMemory<byte>(size);
			Entries.Add(data);

			var dataMemory = data.Memory;
			var offset = 0;
			memory = memory[(Unsafe.SizeOf<ForgeBundleHeader>() + Unsafe.SizeOf<ForgeBundleBlock>() * header.BlockCount)..];
			foreach (var block in blocks) {
				_ = MemoryMarshal.Read<uint>(span); // checksum
				var compressedBlock = memory[4..(4 + span.Length)];
				memory = memory[(4 + compressedBlock.Length)..];

				offset += block.UncompressedSize;
				var targetBlock = dataMemory.Slice(offset, block.UncompressedSize);

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
					                            ForgeCompressionType.Zstd => CompressionType.Zstd,
					                            _ => throw new NotSupportedException(),
				                            };

				CompressionHelper.Decompress(helperCompressionType, compressedBlock, targetBlock);
			}

			span = memory.Span;
		}
	}

	public ForgeFile File { get; }
	public ObjectId UId { get; }
	public List<RentedMemory<byte>> Entries { get; } = [];

	public void Dispose() {
		foreach (var entry in Entries) {
			entry.Dispose();
		}
	}
}
