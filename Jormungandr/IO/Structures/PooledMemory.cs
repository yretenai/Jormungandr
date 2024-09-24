using System.Buffers;

namespace Jormungandr.IO.Structures;

public sealed class PooledMemory<T>(int length) : RentedMemory<T> {
	public override int Length { get; } = length;
	public IMemoryOwner<T> Owner { get; } = MemoryPool<T>.Shared.Rent(length);
	public override Memory<T> Memory => Owner.Memory[..Length];
	public override Span<T> Span => Memory.Span;

	public override void Dispose() {
		Owner.Dispose();
	}

	public override IEnumerator<T> GetEnumerator() {
		for (var i = 0; i < Length; ++i) {
			yield return Memory.Span[i];
		}
	}
}
