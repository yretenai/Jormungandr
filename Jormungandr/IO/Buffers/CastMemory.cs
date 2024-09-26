namespace Jormungandr.IO.Buffers;

// typecast cast memory owner.
public class CastMemory<T>(RentedMemory<byte> Owner, int Offset, int Length) : RentedMemory<T> where T : struct {
	public RentedMemory<byte> Owner { get; } = Owner;
	public override int Length { get; } = Length;
	public int Offset { get; } = Offset;
	public MemoryTypeManager<T> Typed { get; private set; } = new(Owner.Memory);
	public override Span<T> Span => Memory.Span;
	public override Memory<T> Memory => Typed.Memory.Slice(Offset, Length);

	protected override void Dispose(bool disposing) {
		((IDisposable) Typed).Dispose();
		Typed = null!;
		base.Dispose(disposing);
	}
}
