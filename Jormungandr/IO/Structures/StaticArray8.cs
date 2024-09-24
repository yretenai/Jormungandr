using System.Runtime.CompilerServices;

namespace Jormungandr.IO.Structures;

[InlineArray(8)]
public struct StaticArray8 : IEquatable<StaticArray8>, IEquatable<Span<byte>>, IEquatable<ReadOnlySpan<byte>>,
                             IComparable<StaticArray8>, IComparable<Span<byte>>, IComparable<ReadOnlySpan<byte>> {
	public byte Value;

	public bool Equals(StaticArray8 other) => ((Span<byte>) other).SequenceEqual(this);
	public int CompareTo(StaticArray8 other) => ((Span<byte>) other).SequenceCompareTo(this);
	public bool Equals(Span<byte> other) => other.SequenceEqual(this);
	public int CompareTo(Span<byte> other) => other.SequenceCompareTo(this);
	public bool Equals(ReadOnlySpan<byte> other) => other.SequenceEqual(this);
	public int CompareTo(ReadOnlySpan<byte> other) => other.SequenceCompareTo(this);
	public override bool Equals(object? obj) => obj is StaticArray8 other && Equals(other);

	public override int GetHashCode() {
		var hc = new HashCode();
		hc.AddBytes((Span<byte>) this);
		return hc.ToHashCode();
	}

	public static bool operator ==(StaticArray8 left, StaticArray8 right) => left.Equals(right);
	public static bool operator !=(StaticArray8 left, StaticArray8 right) => !(left == right);
	public static bool operator ==(StaticArray8 left, Span<byte> right) => left.Equals(right);
	public static bool operator !=(StaticArray8 left, Span<byte> right) => !(left == right);
	public static bool operator ==(StaticArray8 left, ReadOnlySpan<byte> right) => left.Equals(right);
	public static bool operator !=(StaticArray8 left, ReadOnlySpan<byte> right) => !(left == right);
}
