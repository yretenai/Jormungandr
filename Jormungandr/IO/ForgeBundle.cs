using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jormungandr.IO.Structures;
using Jormungandr.Scimitar;
using Watersports.Compression;

namespace Jormungandr.IO;

public sealed class ForgeBundle : IDisposable {
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
			if (header.Magic >> 8 != 0x57FBAA) {
				break;
			}

			Debug.Assert(header.CompressionType is >= ForgeCompressionType.OodleKraken and <= ForgeCompressionType.OodleSelkieOpt);

			span = span[Unsafe.SizeOf<ForgeBundleHeader>()..];
			if (header.BlockCount == 0) {
				Streams.Add([]);
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
			Streams.Add(data);

			var dataMemory = data.Memory;
			var offset = 0;
			memory = memory[(Unsafe.SizeOf<ForgeBundleHeader>() + Unsafe.SizeOf<ForgeBundleBlock>() * header.BlockCount)..];
			foreach (var block in blocks) {
				_ = MemoryMarshal.Read<uint>(span); // checksum
				var compressedBlock = memory[4..(4 + block.CompressedSize)];
				memory = memory[(4 + block.CompressedSize)..];

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
					                            ForgeCompressionType.Zstd => CompressionType.Zstd,
					                            _ => throw new NotSupportedException(),
				                            };

				CompressionHelper.Decompress(helperCompressionType, compressedBlock, targetBlock);
			}

			span = memory.Span;
		}

		if (span.Length > 0) {
			var remainder = new PooledMemory<byte>(span.Length);
			span.CopyTo(remainder.Span);
			Streams.Add(remainder);
			Assets.Add(new SloppyMemory<byte>(remainder, 0, span.Length));
		} else {
			Debug.Assert(Streams.Count == 2);
			Debug.Assert(Streams[0].Length >= Unsafe.SizeOf<ForgeBundleEntry>());

			var offset = 0;
			var dataBuffer = Streams[1];
			foreach (var header in Headers) {
				Assets.Add(new SloppyMemory<byte>(dataBuffer, offset, header.Size));
				offset += header.Size;
			}
		}
	}

	public ForgeBundle(ForgeFile forgeFile, ObjectId uid) {
		File = forgeFile;
		UId = uid;
	}

	private static int MinimumBlockSize { get; } = Unsafe.SizeOf<ForgeBundleHeader>() + Unsafe.SizeOf<ForgeBundleBlock>() + sizeof(uint);

	public ForgeFile File { get; }
	public ObjectId UId { get; }

	public Span<ForgeBundleEntry> Headers => Streams.Count <= 2 ? [] : MemoryMarshal.Cast<byte, ForgeBundleEntry>(Streams[0].Span);
	public List<SloppyMemory<byte>> Assets { get; } = [];
	private List<RentedMemory<byte>> Streams { get; } = [];

	public void Dispose() {
		foreach (var entry in Streams) {
			entry.Dispose();
		}
	}
}
