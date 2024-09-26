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
	ZStandard,
	ZStandardOpt,
}
