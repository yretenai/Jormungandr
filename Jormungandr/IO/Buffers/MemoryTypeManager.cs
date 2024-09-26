using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Jormungandr.IO.Buffers;

// memory manager for casting memory
public class MemoryTypeManager<T>(Memory<byte> buffer) : MemoryManager<T> where T : struct {
	private MemoryHandle? Handle { get; set; }
	private int RefCount { get; set; }
	private Memory<byte> Buffer { get; set; } = buffer;
	private bool Disposed { get; set; }

	public override Span<T> GetSpan() {
		ObjectDisposedException.ThrowIf(Disposed, this);
		return MemoryMarshal.Cast<byte, T>(Buffer.Span);
	}

	public override MemoryHandle Pin(int elementIndex = 0) {
		ObjectDisposedException.ThrowIf(Disposed, this);
		if (elementIndex < 0) {
			throw new IndexOutOfRangeException();
		}

		var byteIndex = Unsafe.SizeOf<T>() * elementIndex;
		if (byteIndex >= Buffer.Length) { }

		RefCount++;
		Handle ??= Memory.Pin();

		unsafe {
			return new MemoryHandle(((nint) Handle.Value.Pointer + byteIndex).ToPointer(), default, this);
		}
	}

	public override void Unpin() {
		ObjectDisposedException.ThrowIf(Disposed, this);
		RefCount--;
		if (RefCount <= 0) {
			RefCount = 0;
			Handle?.Dispose();
			Handle = null;
		}
	}

	protected override bool TryGetArray(out ArraySegment<T> segment) {
		ObjectDisposedException.ThrowIf(Disposed, this);
		return MemoryMarshal.TryGetArray(Memory, out segment);
	}

	protected override void Dispose(bool disposing) {
		ObjectDisposedException.ThrowIf(Disposed, this);
		Disposed = true;
		Handle?.Dispose();
		Buffer = null!;
	}
}
