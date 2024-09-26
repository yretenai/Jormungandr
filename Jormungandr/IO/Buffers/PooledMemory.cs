using System.Buffers;

namespace Jormungandr.IO.Buffers;

// owning rented memory owner.
public sealed class PooledMemory<T>(int length) : RentedMemory<T> where T : struct {
	public override int Length { get; } = length;
	public IMemoryOwner<T> Owner { get; } = MemoryPool<T>.Shared.Rent(length);
	public override Memory<T> Memory => Owner.Memory[..Length];
	public override Span<T> Span => Memory.Span;

	protected override void Dispose(bool disposing) {
		if (disposing) {
			Owner.Dispose();
		}
	}
}
