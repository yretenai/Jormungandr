using System.Collections;

namespace Jormungandr.IO.Structures;

public class RentedMemory<T> : IDisposable, IEnumerable<T> {
	public static RentedMemory<T> Empty { get; } = new();

	public virtual int Length => 0;
	public virtual Memory<T> Memory { get; } = Memory<T>.Empty;
	public virtual Span<T> Span => Span<T>.Empty;

	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	public virtual IEnumerator<T> GetEnumerator() => Enumerable.Empty<T>().GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	protected virtual void Dispose(bool disposing) {
		if (disposing) { }
	}
}
