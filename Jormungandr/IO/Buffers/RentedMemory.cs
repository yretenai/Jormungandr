using System.Collections;

namespace Jormungandr.IO.Buffers;

// owning zero length memory owner.
public class RentedMemory<T> : IDisposable, IEnumerable<T> where T : struct {
	public static RentedMemory<T> Empty { get; } = new();

	public virtual int Length => 0;
	public virtual Memory<T> Memory { get; } = Memory<T>.Empty;
	public virtual Span<T> Span => Span<T>.Empty;

	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	public virtual IEnumerator<T> GetEnumerator() {
		for (var i = 0; i < Length; ++i) {
			yield return Span[i];
		}
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	protected virtual void Dispose(bool disposing) {
		if (disposing) { }
	}
}
