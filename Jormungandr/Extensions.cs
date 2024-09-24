using System.Runtime.InteropServices;

namespace Jormungandr;

public static class Extensions {
	public static void ReadExactly<T>(this Stream stream, ref T value) where T : struct => stream.ReadExactly(MemoryMarshal.AsBytes(new Span<T>(ref value)));
	public static void ReadExactly<T>(this Stream stream, Span<T> value) where T : struct => stream.ReadExactly(MemoryMarshal.AsBytes(value));

	public static T ReadExactly<T>(this Stream stream) where T : struct {
		T value = default;
		stream.ReadExactly(MemoryMarshal.AsBytes(new Span<T>(ref value)));
		return value;
	}
}
