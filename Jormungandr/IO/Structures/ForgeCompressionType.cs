namespace Jormungandr.IO.Structures;

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
}
