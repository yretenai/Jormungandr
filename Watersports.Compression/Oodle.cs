using System;
using System.Buffers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Serilog;

namespace Watersports.Compression;

public static partial class Oodle {
	public enum OodleLZ_Profile {
		OodleLZ_Profile_Main = 0, // Main profile (all current features allowed)
		OodleLZ_Profile_Reduced = 1, // Reduced profile (Kraken only, limited feature set)
	}

	public enum OodleLZ_Jobify {
		Default = 0, // Use compressor default for level of internal job usage
		Disable = 1, // Don't use jobs at all
		Normal = 2, // Try to balance parallelism with increased memory usage
		Aggressive = 3, // Maximize parallelism even when doing so requires large amounts of memory
	}

	public enum OodleLZ_CompressionLevel {
		None = 0, // don't compress, just copy raw bytes
		SuperFast = 1, // super fast mode, lower compression ratio
		VeryFast = 2, // fastest LZ mode with still decent compression ratio
		Fast = 3, // fast - good for daily use
		Normal = 4, // standard medium speed LZ mode

		Optimal1 = 5, // optimal parse level 1 (faster optimal encoder)
		Optimal2 = 6, // optimal parse level 2 (recommended baseline optimal encoder)
		Optimal3 = 7, // optimal parse level 3 (slower optimal encoder)
		Optimal4 = 8, // optimal parse level 4 (very slow optimal encoder)
		Optimal5 = 9, // optimal parse level 5 (don't care about encode speed, maximum compression)

		HyperFast1 = -1, // faster than SuperFast, less compression
		HyperFast2 = -2, // faster than HyperFast1, less compression
		HyperFast3 = -3, // faster than HyperFast2, less compression
		HyperFast4 = -4, // fastest, less compression

		HyperFast = HyperFast1, // alias hyperfast base level
		Optimal = Optimal2, // alias optimal standard level
		Max = Optimal5, // maximum compression level
		Min = HyperFast4, // fastest compression level

		Invalid = 0x40000000,
	}

	public enum OodleLZ_Compressor {
		Invalid = -1,
		LZH = 0, // no longer supported as of Oodle 2.9.0
		LZHLW = 1, // no longer supported as of Oodle 2.9.0
		LZNIB = 2, // no longer supported as of Oodle 2.9.0
		None = 3, // None = memcpy, pass through uncompressed bytes
		LZB16 = 4, // DEPRECATED but still supported
		LZBLW = 5, // no longer supported as of Oodle 2.9.0
		LZA = 6, // no longer supported as of Oodle 2.9.0
		LZNA = 7, // no longer supported as of Oodle 2.9.0
		Kraken = 8, // Fast decompression and high compression ratios, amazing!
		Mermaid = 9, // Mermaid is between Kraken & Selkie - crazy fast, still decent compression.
		BitKnit = 10, // no longer supported as of Oodle 2.9.0
		Selkie = 11, // Selkie is a super-fast relative of Mermaid.  For maximum decode speed.
		Hydra = 12, // Hydra, the many-headed beast = Leviathan, Kraken, Mermaid, or Selkie (see $OodleLZ_About_Hydra)
		Leviathan = 13, // Leviathan = Kraken's big brother with higher compression, slightly slower decompression.
	}

	public enum OodleLZ_Decode_ThreadPhase {
		ThreadPhase1 = 1,
		ThreadPhase2 = 2,
		ThreadPhaseAll = 3,
		Unthreaded = ThreadPhaseAll,
	}

	public enum OodleLZ_Verbosity {
		None = 0,
		Minimal = 1,
		Some = 2,
		Lots = 3,
	}

	static Oodle() {
		// not as a dllimport because these may not exist.
		var handle = CompressionHelper.DllImportResolver(NativeMethods.LIBRARY_NAME, Assembly.GetExecutingAssembly(), DllImportSearchPath.SafeDirectories);
		if (handle != IntPtr.Zero) {
			if (NativeLibrary.TryGetExport(handle, "OodleCore_Plugin_Printf_Verbose", out var callbackAddress)) {
				var callback = Marshal.GetDelegateForFunctionPointer<NativeMethods.OodleCore_Plugin_Printf>(callbackAddress);
				NativeMethods.OodleCore_Plugins_SetPrintf(callback);
			} else if (NativeLibrary.TryGetExport(handle, "OodleCore_Plugin_Printf_Default", out callbackAddress)) {
				var callback = Marshal.GetDelegateForFunctionPointer<NativeMethods.OodleCore_Plugin_Printf>(callbackAddress);
				NativeMethods.OodleCore_Plugins_SetPrintf(callback);
			}

			if (NativeLibrary.TryGetExport(handle, "OodleCore_Plugin_DisplayAssertion_Default", out callbackAddress)) {
				var callback = Marshal.GetDelegateForFunctionPointer<NativeMethods.OodleCore_Plugin_DisplayAssertion>(callbackAddress);
				NativeMethods.OodleCore_Plugins_SetAssertion(callback);
			}
		}

		NativeMethods.Oodle_LogHeader();

		var version = 0u;
		var expected = CreateOodleVersion(9, 0);
		if (NativeMethods.Oodle_CheckVersion(expected, ref version) != 1) {
			Log.Error("Invalid Oodle version! Expected a version compatible with {Expected} ({Version:X8}), got {Parsed} ({Result:X8})", ParseOodleVersion(expected), expected, ParseOodleVersion(version), version);
		} else {
			Log.Information("Loaded Oodle Version {Version}", ParseOodleVersion(version));
		}

		BlockDecoderMemorySizeNeeded = NativeMethods.OodleLZDecoder_MemorySizeNeeded(OodleLZ_Compressor.Invalid, -1);
	}

	public static int BlockDecoderMemorySizeNeeded { get; }

	public static string ParseOodleVersion(uint value) {
		var check = value >> 28;
		var provider = (value >> 24) & 0xF;
		var major = (value >> 16) & 0xFF;
		var minor = (value >> 8) & 0xFF;
		var table = value & 0xFF;
		return $"{check}.{major}.{minor} (provider: {provider:X1}, seek: {table})";
	}

	public static uint CreateOodleVersion(int major, int minor, int seekTableSize = 48) => (46u << 24) | (uint) (major << 16) | (uint) (minor << 8) | (uint) seekTableSize;

	public static unsafe int Decompress(Memory<byte> input, Memory<byte> output) {
		using var inPin = input.Pin();
		using var outPin = output.Pin();
		using var pool = MemoryPool<byte>.Shared.Rent(BlockDecoderMemorySizeNeeded);
		using var poolPin = pool.Memory.Pin();
		return NativeMethods.OodleLZ_Decompress((byte*) inPin.Pointer, input.Length, (byte*) outPin.Pointer, output.Length, true, false, OodleLZ_Verbosity.Minimal, null, 0, null, null, (byte*) poolPin.Pointer, BlockDecoderMemorySizeNeeded, OodleLZ_Decode_ThreadPhase.Unthreaded);
	}

	public static IMemoryOwner<byte>? Decompress(Memory<byte> input, MemoryPool<byte>? pool = null) {
		var size = GetDecodeBufferSize(input, false);
		var output = (pool ?? MemoryPool<byte>.Shared).Rent(size);
		if (Decompress(input, output.Memory[..size]) != -1) {
			return output;
		}

		output.Dispose();
		return null;
	}

	public static IMemoryOwner<byte>? Compress(Memory<byte> input, OodleLZ_Compressor compressor, OodleLZ_CompressionLevel level, MemoryPool<byte>? pool = null) {
		var size = GetCompressedBufferSize(compressor, input.Length);
		var output = (pool ?? MemoryPool<byte>.Shared).Rent(size);
		if (Compress(input, output.Memory[..size], compressor, level) != -1) {
			return output;
		}

		output.Dispose();
		return null;
	}

	private static int Compress(Memory<byte> input, Memory<byte> output, OodleLZ_Compressor compressor, OodleLZ_CompressionLevel level) {
		var options = GetDefaultOptions(compressor, level);
		return Compress(input, output, Memory<byte>.Empty, compressor, level, options);
	}

	private static unsafe int Compress(Memory<byte> input, Memory<byte> output, Memory<byte> dict, OodleLZ_Compressor compressor, OodleLZ_CompressionLevel level, OodleLZ_CompressOptions options) {
		var compressorOptions = options;
		compressorOptions.Unused1 = compressorOptions.Unused2 = compressorOptions.Unused3 = compressorOptions.Unused4 = compressorOptions.Unused5 = compressorOptions.Unused6 = 0;

		int scratchBound;
		fixed (OodleLZ_CompressOptions* compressorOptionsPin = &Unsafe.AsRef(ref compressorOptions)) {
			scratchBound = (int) NativeMethods.OodleLZ_GetCompressScratchMemBound(compressor, level, input.Length + compressorOptions.DictionarySize, compressorOptionsPin);
		}

		if (scratchBound == -1) {
			scratchBound = BlockDecoderMemorySizeNeeded;
		}

		options.DictionarySize = dict.Length;

		using var inPin = input.Pin();
		using var outPin = output.Pin();
		using var dictPin = dict.Pin();
		using var scratch = MemoryPool<byte>.Shared.Rent(scratchBound);
		using var scratchPin = scratch.Memory.Pin();
		fixed (OodleLZ_CompressOptions* compressorOptionsPin = &Unsafe.AsRef(ref compressorOptions)) {
			return NativeMethods.OodleLZ_Compress(compressor, (byte*) inPin.Pointer, input.Length, (byte*) outPin.Pointer, level, compressorOptionsPin, (byte*) dictPin.Pointer, nint.Zero, (byte*) scratchPin.Pointer, scratchBound);
		}
	}

	public static unsafe int Compress(Memory<byte> input, Memory<byte> output) {
		using var inPin = input.Pin();
		using var outPin = output.Pin();
		using var pool = MemoryPool<byte>.Shared.Rent(BlockDecoderMemorySizeNeeded);
		using var poolPin = pool.Memory.Pin();
		return NativeMethods.OodleLZ_Decompress((byte*) inPin.Pointer, input.Length, (byte*) outPin.Pointer, output.Length, true, false, OodleLZ_Verbosity.Minimal, null, 0, null, null, (byte*) poolPin.Pointer, BlockDecoderMemorySizeNeeded, OodleLZ_Decode_ThreadPhase.Unthreaded);
	}

	private static unsafe OodleLZ_CompressOptions GetDefaultOptions(OodleLZ_Compressor compressor, OodleLZ_CompressionLevel level) {
		var options = Unsafe.Read<OodleLZ_CompressOptions>(NativeMethods.OodleLZ_CompressOptions_GetDefault(compressor, level));
		options.Unused1 = options.Unused2 = options.Unused3 = options.Unused4 = options.Unused5 = options.Unused6 = 0;
		return options;
	}

	public static int GetDecodeBufferSize(Memory<byte> input, bool corruptionPossible) {
		return (int) NativeMethods.OodleLZ_GetDecodeBufferSize(GetCompressor(input), input.Length, corruptionPossible);
	}

	public static int GetCompressedBufferSize(OodleLZ_Compressor compressor, int length) {
		return (int) NativeMethods.OodleLZ_GetCompressedBufferSizeNeeded(compressor, length);
	}

	public static unsafe OodleLZ_Compressor GetCompressor(Memory<byte> input) {
		using var inPin = input.Pin();
		var independent = false;
		return NativeMethods.OodleLZ_GetFirstChunkCompressor((byte*) inPin.Pointer, input.Length, ref independent);
	}

	public static string GetCompressorName(Memory<byte> input) {
		var compressor = GetCompressor(input);
		return GetCompressorName(compressor);
	}

	public static string GetCompressorName(OodleLZ_Compressor compressor) => NativeMethods.OodleLZ_Compressor_GetName(compressor);

	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public record struct OodleLZ_CompressOptions {
		public int Unused1 { get; set; } // unused ; was verbosity (set to zero)
		public int MinMatchLen { get; set; } // minimum match length ; cannot be used to reduce a compressor's default MML, but can be higher.  On some types of data, a large MML (6 or 8) is a space-speed win.
		public bool SeekChunkReset { get; set; } // whether chunks should be independent, for seeking and parallelism
		public int SeekChunkLen { get; set; } // length of independent seek chunks (if seekChunkReset) ; must be a power of 2 and >= $OODLELZ_BLOCK_LEN ; you can use $OodleLZ_MakeSeekChunkLen
		public OodleLZ_Profile Profile { get; set; } // decoder profile to target (set to zero)
		public int DictionarySize { get; set; } // sets a maximum offset for matches, if lower than the maximum the format supports.  <= 0 means infinite (use whole buffer).  Often power of 2 but doesn't have to be.
		public int SpaceSpeedTradeoffBytes { get; set; } // this is a number of bytes; I must gain at least this many bytes of compressed size to accept a speed-decreasing decision
		public int Unused2 { get; set; } //  unused ; was maxHuffmansPerChunk
		public bool SendQuantumCRCs { get; set; } // should the encoder send a CRC of each compressed quantum, for integrity checks; this is necessary if you want to use OodleLZ_CheckCRC_Yes on decode
		public int MaxLocalDictionarySize { get; set; } // (Optimals) size of local dictionary before needing a long range matcher.  This does not set a window size for the decoder; it's useful to limit memory use and time taken in the encoder.  maxLocalDictionarySize must be a power of 2.  Must be <= OODLELZ_LOCALDICTIONARYSIZE_MAX
		public bool MakeLongRangeMatcher { get; set; } // (Optimals) should the encoder find matches beyond maxLocalDictionarySize using an LRM
		public int MatchTableSizeLog2 { get; set; } //(non-Optimals)  when variable, sets the size  of the match finder structure (often a hash table) ; use 0 for the compressor's default
		public OodleLZ_Jobify Jobify { get; set; } // controls internal job usage by compressors
		public nint JobifyUserPtr { get; set; } // user pointer passed through to RunJob and WaitJob callbacks
		public int FarMatchMinLen { get; set; } // far matches must be at least this len
		public int FarMatchOffsetLog2 { get; set; } // if not zero, the log2 of an offset that must meet farMatchMinLen
		public int Unused3 { get; set; } // reserved space for adding more options; zero these!
		public int Unused4 { get; set; } // reserved space for adding more options; zero these!
		public int Unused5 { get; set; } // reserved space for adding more options; zero these!
		public int Unused6 { get; set; } // reserved space for adding more options; zero these!
	}

	private static partial class NativeMethods {
		[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		public delegate int OodleCore_Plugin_DisplayAssertion([MarshalAs(UnmanagedType.LPStr)] string file, int line, [MarshalAs(UnmanagedType.LPStr)] string function, [MarshalAs(UnmanagedType.LPStr)] string message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		public delegate int OodleCore_Plugin_Printf(int verboseLevel, [MarshalAs(UnmanagedType.LPStr)] string file, int line, [MarshalAs(UnmanagedType.LPStr)] string format);

		internal const string LIBRARY_NAME = "oo2core";

		[LibraryImport(LIBRARY_NAME), DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
		public static unsafe partial int OodleLZ_Decompress(byte* srcBuf, long srcSize, byte* rawBuf, long rawSize, [MarshalAs(UnmanagedType.I4)] bool fuzzSafe, [MarshalAs(UnmanagedType.I4)] bool checkCRC, OodleLZ_Verbosity verbosity, byte* decBufBase, long decBufSize, void* fpCallback, void* callbackUserData, byte* decoderMemory, long decoderMemorySize, OodleLZ_Decode_ThreadPhase threadPhase);

		[LibraryImport(LIBRARY_NAME), DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
		public static unsafe partial int OodleLZ_Compress(OodleLZ_Compressor compressor, byte* rawBuf, long rawSize, byte* compBuf, OodleLZ_CompressionLevel level, OodleLZ_CompressOptions* options, byte* dictionaryBase, nint lrm, byte* scratchMem, long scratchSize);

		[LibraryImport(LIBRARY_NAME), DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
		public static unsafe partial OodleLZ_Compressor OodleLZ_GetFirstChunkCompressor(byte* srcBuf, long srcSize, [MarshalAs(UnmanagedType.I4)] ref bool independent);

		[LibraryImport(LIBRARY_NAME), DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
		[return: MarshalAs(UnmanagedType.LPStr)]
		public static partial string OodleLZ_Compressor_GetName(OodleLZ_Compressor compressor);

		[LibraryImport(LIBRARY_NAME), DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
		public static partial void Oodle_LogHeader();

		[LibraryImport(LIBRARY_NAME), DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
		public static partial int Oodle_CheckVersion(uint oodleHeaderVersion, ref uint oodleLibVersion);

		[LibraryImport(LIBRARY_NAME), DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
		[return: MarshalAs(UnmanagedType.FunctionPtr)]
		public static partial void OodleCore_Plugins_SetPrintf([MarshalAs(UnmanagedType.FunctionPtr)] OodleCore_Plugin_Printf rrRawPrintf);

		[LibraryImport(LIBRARY_NAME), DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
		[return: MarshalAs(UnmanagedType.FunctionPtr)]
		public static partial void OodleCore_Plugins_SetAssertion([MarshalAs(UnmanagedType.FunctionPtr)] OodleCore_Plugin_DisplayAssertion rrDisplayAssertion);

		[LibraryImport(LIBRARY_NAME), DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
		public static partial int OodleLZDecoder_MemorySizeNeeded(OodleLZ_Compressor compressor, long size);

		[LibraryImport(LIBRARY_NAME), DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
		public static partial long OodleLZ_GetCompressedBufferSizeNeeded(OodleLZ_Compressor compressor, long size);

		[LibraryImport(LIBRARY_NAME), DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
		public static partial long OodleLZ_GetDecodeBufferSize(OodleLZ_Compressor compressor, long size, [MarshalAs(UnmanagedType.I4)] bool corruptionPossible);

		[LibraryImport(LIBRARY_NAME), DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
		public static unsafe partial long OodleLZ_GetCompressScratchMemBound(OodleLZ_Compressor compressor, OodleLZ_CompressionLevel level, long size, OodleLZ_CompressOptions* options);

		[LibraryImport(LIBRARY_NAME), DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
		public static unsafe partial OodleLZ_CompressOptions* OodleLZ_CompressOptions_GetDefault(OodleLZ_Compressor compressor, OodleLZ_CompressionLevel level);
	}
}
