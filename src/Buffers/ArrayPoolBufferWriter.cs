using System.Buffers;
using System.Diagnostics;

namespace Eryri.Buffers;

/// <summary>
/// Represents a pooled, array-backed output sink into which <typeparam name="T"/> data can be written.
/// </summary>
/// <remarks>
/// The maximum stream length is <see cref="int.MaxValue"/>.
/// </remarks>
public sealed class ArrayPoolBufferWriter<T> : IBufferWriter<T>, IDisposable
{
    private readonly bool _clearOnReturn;
    private T[] _rentedBuffer;
    private int _index;

    private const int MinimumBufferSize = 256;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayPoolBufferWriter{T}"/> class,
    /// in which data can be written to, with the default initial capacity.
    /// </summary>
    public ArrayPoolBufferWriter(bool clearOnReturn = false) : this(MinimumBufferSize, clearOnReturn) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayPoolBufferWriter{T}"/> class,
    /// in which data can be written to, with an initial capacity specified.
    /// </summary>
    /// <param name="initialCapacity">The minimum capacity with which to initialize the underlying buffer.</param>
    /// <param name="clearOnReturn">Decides if the rented arrays are cleared when they're returned to the pool.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="initialCapacity"/> is not positive (i.e. less than or equal to 0).
    /// </exception>
    public ArrayPoolBufferWriter(int initialCapacity, bool clearOnReturn = false)
    {
        if (initialCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialCapacity), actualValue: initialCapacity, $"{nameof(initialCapacity)} ('{initialCapacity}') must be a non-negative and non-zero value.");
        }

        _clearOnReturn = clearOnReturn;
        _rentedBuffer = ArrayPool<T>.Shared.Rent(initialCapacity);
        _index = 0;
    }

    /// <summary>
    /// Gets the data written to the underlying buffer so far, as a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    public ReadOnlySpan<T> WrittenSpan
    {
        get
        {
            ThrowIfDisposed();
            return _rentedBuffer.AsSpan(0, _index);
        }
    }

    /// <summary>
    /// Gets the data written to the underlying buffer so far, as a <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    public ReadOnlyMemory<T> WrittenMemory
    {
        get
        {
            ThrowIfDisposed();
            return _rentedBuffer.AsMemory(0, _index);
        }
    }

    /// <summary>
    /// Gets the amount of data written to the underlying buffer so far.
    /// </summary>
    public int WrittenCount
    {
        get
        {
            ThrowIfDisposed();
            return _index;
        }
    }

    /// <summary>
    /// Gets the total amount of space within the underlying buffer.
    /// </summary>
    public int Capacity
    {
        get
        {
            ThrowIfDisposed();

            return _rentedBuffer.Length;
        }
    }

    /// <summary>
    /// Gets the amount of space available that can still be written into without forcing the underlying buffer to grow.
    /// </summary>
    public int FreeCapacity
    {
        get
        {
            ThrowIfDisposed();

            return _rentedBuffer.Length - _index;
        }
    }

    /// <summary>
    /// Clears the data written to the underlying buffer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// You must reset or clear the <see cref="ArrayPoolBufferWriter{T}"/> before trying to re-use it.
    /// </para>
    /// <para>
    /// The <see cref="ResetWrittenCount"/> method is faster since it only sets to zero the writer's index
    /// while the <see cref="Clear"/> method additionally zeroes the content of the underlying buffer.
    /// </para>
    /// </remarks>
    /// <seealso cref="ResetWrittenCount"/>
    public void Clear()
    {
        ThrowIfDisposed();
        _rentedBuffer.AsSpan(0, _index).Clear();
        _index = 0;
    }

    /// <summary>
    /// Resets the data written to the underlying buffer without zeroing its content.
    /// </summary>
    /// <remarks>
    /// <para>
    /// You must reset or clear the <see cref="ArrayPoolBufferWriter{T}"/> before trying to re-use it.
    /// </para>
    /// <para>
    /// If you reset the writer using the <see cref="ResetWrittenCount"/> method, the underlying buffer will not be cleared.
    /// </para>
    /// </remarks>
    /// <seealso cref="Clear"/>
    public void ResetWrittenCount() => _index = 0;

    /// <summary>
    /// Returns the rented buffer back to the pool.
    /// </summary>
    public void Dispose()
    {
        if (_rentedBuffer == null)
        {
            return;
        }

        ArrayPool<T>.Shared.Return(_rentedBuffer, _clearOnReturn);
        _rentedBuffer = null!;
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_rentedBuffer == null, this);

    /// <summary>
    /// Notifies <see cref="IBufferWriter{T}"/> that <paramref name="count"/> amount of data was written to the output <see cref="Span{T}"/>/<see cref="Memory{T}"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="count"/> is negative.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to advance past the end of the underlying buffer.
    /// </exception>
    /// <remarks>
    /// You must request a new buffer after calling Advance to continue writing more data and cannot write to a previously acquired buffer.
    /// </remarks>
    public void Advance(uint count) => Advance((int)count);

    public void Advance(int count)
    {
        if (count == 0)
        {
            // no-op
            return;
        }

        ThrowIfDisposed();
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), actualValue: count, $"{nameof(count)} ('{count}') must be a non-negative value.");
        }

        if (_index > _rentedBuffer.Length - count)
        {
            ThrowInvalidOperationException(_rentedBuffer.Length);
        }

        _index += count;
    }

    /// <summary>
    /// Returns a <see cref="Memory{T}"/> to write to that is at least the requested length (specified by <paramref name="sizeHint"/>).
    /// If no <paramref name="sizeHint"/> is provided (or it's equal to 0), some non-empty buffer is returned.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sizeHint"/> is negative.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This will never return an empty <see cref="Memory{T}"/>.
    /// </para>
    /// <para>
    /// There is no guarantee that successive calls will return the same buffer or the same-sized buffer.
    /// </para>
    /// <para>
    /// You must request a new buffer after calling Advance to continue writing more data and cannot write to a previously acquired buffer.
    /// </para>
    /// <para>
    /// If you reset the writer using the <see cref="ResetWrittenCount"/> method, this method may return a non-cleared <see cref="Memory{T}"/>.
    /// </para>
    /// <para>
    /// If you clear the writer using the <see cref="Clear"/> method, this method will return a <see cref="Memory{T}"/> with its content zeroed.
    /// </para>
    /// </remarks>
    public Memory<T> GetMemory(int sizeHint = 0)
    {
        ThrowIfDisposed();

        CheckAndResizeBuffer(sizeHint);
        return _rentedBuffer.AsMemory(_index);
    }

    /// <summary>
    /// Returns a <see cref="Span{T}"/> to write to that is at least the requested length (specified by <paramref name="sizeHint"/>).
    /// If no <paramref name="sizeHint"/> is provided (or it's equal to 0), some non-empty buffer is returned.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sizeHint"/> is negative.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method never returns an empty <see cref="Span{T}"/>.
    /// </para>
    /// <para>
    /// There is no guarantee that successive calls return the same underlying buffer or a span of the same length.
    /// </para>
    /// <para>
    /// A call may resize the underlying buffer. Any previously obtained span or memory that refers to the buffer must not be used after a subsequent call to this method.
    /// </para>
    /// <para>
    /// The returned span covers the writable region beginning at the current written position. Its contents are not guaranteed to be initialized.
    /// </para>
    /// <para>
    /// After calling <see cref="ResetWrittenCount"/>, the contents of the underlying buffer are retained and may be exposed by subsequent calls.
    /// </para>
    /// <para>
    /// After calling <see cref="Clear"/>, the previously written region is cleared. However, the entire returned span is not guaranteed to contain default values.
    /// </para>
    /// </remarks>
    public Span<T> GetSpan(int sizeHint = 0)
    {
        ThrowIfDisposed();

        CheckAndResizeBuffer(sizeHint);
        return _rentedBuffer.AsSpan(_index);
    }

    private void CheckAndResizeBuffer(int sizeHint)
    {
        Debug.Assert(_rentedBuffer != null);

        if (sizeHint < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeHint), actualValue: sizeHint, $"{nameof(sizeHint)} ('{sizeHint}') must be a non-negative value.");
        }

        if (sizeHint == 0)
        {
            sizeHint = MinimumBufferSize;
        }

        var availableSpace = _rentedBuffer.Length - _index;

        if (sizeHint > availableSpace)
        {
            var growBy = Math.Max(sizeHint, _rentedBuffer.Length);

            var newSize = checked(_rentedBuffer.Length + growBy);

            var oldBuffer = _rentedBuffer;

            _rentedBuffer = ArrayPool<T>.Shared.Rent(newSize);

            Debug.Assert(oldBuffer.Length >= _index);
            Debug.Assert(_rentedBuffer.Length >= _index);

            oldBuffer.AsSpan(0, _index).CopyTo(_rentedBuffer);
            ArrayPool<T>.Shared.Return(oldBuffer, _clearOnReturn);
        }

        Debug.Assert(_rentedBuffer.Length - _index > 0);
        Debug.Assert(_rentedBuffer.Length - _index >= sizeHint);
    }

    private static void ThrowInvalidOperationException(int capacity)
    {
        throw new InvalidOperationException($"Cannot advance past the end of the buffer, which has a size of {capacity}.");
    }
}