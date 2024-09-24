namespace Watersports.Compression;

public enum CompressionType {
	None,
	Oodle,
	Brotli,
	Zlib,
	Gzip,
	LZ4,
	LZ4HC,
	LZO1,
	LZO2,
	LZX,
	LZMA,
	Zstd,
	Density,
	Custom = -1,
}
