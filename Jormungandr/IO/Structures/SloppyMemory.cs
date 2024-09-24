namespace Jormungandr.IO.Structures;

public class SloppyMemory<T>(RentedMemory<T> Owner, int Offset, int Length) : RentedMemory<T> {
	public RentedMemory<T> Owner { get; } = Owner;
	public override int Length { get; } = Length;
	public int Offset { get; } = Offset;
	public override Span<T> Span => Memory.Span;
	public override Memory<T> Memory => Owner.Memory.Slice(Offset, Length);
}
