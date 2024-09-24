using System.Collections;

namespace Jormungandr.IO.Structures;

public class RentedMemory<T> : IDisposable, IEnumerable<T> {
	public static RentedMemory<T> Empty { get; } = new();

	public virtual int Length => 0;
	public virtual Memory<T> Memory { get; } = Memory<T>.Empty;
	public virtual Span<T> Span => Span<T>.Empty;
	public virtual void Dispose() { }
	public virtual IEnumerator<T> GetEnumerator() => Enumerable.Empty<T>().GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
