namespace Jormungandr.IO.Structures;

// compression types for version 29.
public enum ForgeCompressionType : byte {
	Lzo1x,
	Lzo1xOpt,
	Lzo2a,
	LZ4,
	LZ4HC,
	OodleKraken,
	OodleKrakenOpt,
	OodleMermaid,
	OodleMermaidOpt,
	OodleSelkie,
	OodleSelkieOpt,
	None,
	Zlib,
	Zstd,

	// Zstd is added in version 30, replaces Lz4.
	// in Version 28, Zstd is is Oodle
	// in Version 27 and prior, anything past LZ4HC doesn't exist
}
