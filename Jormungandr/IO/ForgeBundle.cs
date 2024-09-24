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
				if (block.IsUncompressed) {
					compressedBlock.CopyTo(targetBlock);
					continue;
				}

				switch (header.CompressionType) {
					case ForgeCompressionType.Lzo1x:
					case ForgeCompressionType.Lzo1xOpt: {
						CompressionHelper.Decompress(CompressionType.LZO1, compressedBlock, targetBlock);
						break;
					}
					case ForgeCompressionType.Lzo2a: {
						CompressionHelper.Decompress(CompressionType.LZO2, compressedBlock, targetBlock);
						break;
					}
					case ForgeCompressionType.LZ4:
					case ForgeCompressionType.LZ4HC: {
						CompressionHelper.Decompress(CompressionType.LZ4, compressedBlock, targetBlock);
						break;
					}
					case ForgeCompressionType.OodleKraken:
					case ForgeCompressionType.OodleKrakenOpt:
					case ForgeCompressionType.OodleMermaid:
					case ForgeCompressionType.OodleMermaidOpt:
					case ForgeCompressionType.OodleSelkie:
					case ForgeCompressionType.OodleSelkieOpt: {
						CompressionHelper.Decompress(CompressionType.Oodle, compressedBlock, targetBlock);
						break;
					}
					case ForgeCompressionType.Zlib: {
						CompressionHelper.Decompress(CompressionType.Zlib, compressedBlock, targetBlock);
						break;
					}
					case ForgeCompressionType.Zstd: {
						CompressionHelper.Decompress(CompressionType.Zstd, compressedBlock, targetBlock);
						break;
					}
					case ForgeCompressionType.None: {
						compressedBlock.CopyTo(targetBlock);
						break;
					}
					default: throw new NotSupportedException();
				}
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
