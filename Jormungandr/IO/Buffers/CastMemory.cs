namespace Jormungandr.IO.Buffers;

// typecast cast memory owner.
public class CastMemory<TTo, TFrom>(RentedMemory<TFrom> Owner, int Offset, int Length) : RentedMemory<TTo> where TTo : struct
	where TFrom : struct {
	public RentedMemory<TFrom> Owner { get; } = Owner;
	public override int Length { get; } = Length;
	public int Offset { get; } = Offset;
	public MemoryTypeManager<TTo, TFrom> Typed { get; private set; } = new(Owner.Memory.Slice(Offset, Length));
	public override Span<TTo> Span => Memory.Span;
	public override Memory<TTo> Memory => Typed.Memory;

	protected override void Dispose(bool disposing) {
		((IDisposable) Typed).Dispose();
		Typed = null!;
		base.Dispose(disposing);
	}
}
