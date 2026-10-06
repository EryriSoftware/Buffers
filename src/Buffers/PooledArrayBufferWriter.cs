using System.Buffers;
using System.Diagnostics;

namespace Eryri.Buffers;

/// <summary>
/// Represents a pooled, array-backed output sink into which <typeparam name="T"/> data can be written.
/// </summary>
public sealed class PooledArrayBufferWriter<T> : IBufferWriter<T>, IDisposable
{
    private T[] _rentedBuffer;
    private int _index;

    private const int MinimumBufferSize = 256;

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledArrayBufferWriter{T}"/> class,
    /// in which data can be written to, with the default initial capacity.
    /// </summary>
    public PooledArrayBufferWriter()
    {
        _rentedBuffer = ArrayPool<T>.Shared.Rent(MinimumBufferSize);
        _index = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledArrayBufferWriter{T}"/> class,
    /// in which data can be written to, with an initial capacity specified.
    /// </summary>
    /// <param name="initialCapacity">The minimum capacity with which to initialize the underlying buffer.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="initialCapacity"/> is not positive (i.e. less than or equal to 0).
    /// </exception>
    public PooledArrayBufferWriter(int initialCapacity)
    {
        if (initialCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialCapacity), actualValue: initialCapacity, $"{nameof(initialCapacity)} ('{initialCapacity}') must be a non-negative and non-zero value.");
        }

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
            CheckIfDisposed();
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
            CheckIfDisposed();
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
            CheckIfDisposed();
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
            CheckIfDisposed();

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
            CheckIfDisposed();

            return _rentedBuffer.Length - _index;
        }
    }

    /// <summary>
    /// Clears the data written to the underlying buffer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// You must reset or clear the <see cref="PooledArrayBufferWriter{T}"/> before trying to re-use it.
    /// </para>
    /// <para>
    /// The <see cref="ResetWrittenCount"/> method is faster since it only sets to zero the writer's index
    /// while the <see cref="Clear"/> method additionally zeroes the content of the underlying buffer.
    /// </para>
    /// </remarks>
    /// <seealso cref="ResetWrittenCount"/>
    public void Clear()
    {
        CheckIfDisposed();

        ClearHelper();
    }

    private void ClearHelper()
    {
        Debug.Assert(_rentedBuffer != null);

        _rentedBuffer.AsSpan(0, _index).Clear();
        _index = 0;
    }

    /// <summary>
    /// Resets the data written to the underlying buffer without zeroing its content.
    /// </summary>
    /// <remarks>
    /// <para>
    /// You must reset or clear the <see cref="PooledArrayBufferWriter{T}"/> before trying to re-use it.
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

        ClearHelper();
        ArrayPool<T>.Shared.Return(_rentedBuffer);
        _rentedBuffer = null!;
    }

    private void CheckIfDisposed()
    {
        if (_rentedBuffer == null)
        {
            ThrowObjectDisposedException();
        }
    }

    private static void ThrowObjectDisposedException()
    {
#if NET
        throw new ObjectDisposedException(nameof(PooledArrayBufferWriter<T>));
#else
        throw new ObjectDisposedException(nameof(IBufferWriter<T>));
#endif
    }

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

        CheckIfDisposed();
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
        CheckIfDisposed();

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
    /// This will never return an empty <see cref="Span{T}"/>.
    /// </para>
    /// <para>
    /// There is no guarantee that successive calls will return the same buffer or the same-sized buffer.
    /// </para>
    /// <para>
    /// You must request a new buffer after calling Advance to continue writing more data and cannot write to a previously acquired buffer.
    /// </para>
    /// <para>
    /// If you reset the writer using the <see cref="ResetWrittenCount"/> method, this method may return a non-cleared <see cref="Span{T}"/>.
    /// </para>
    /// <para>
    /// If you clear the writer using the <see cref="Clear"/> method, this method will return a <see cref="Span{T}"/> with its content zeroed.
    /// </para>
    /// </remarks>
    public Span<T> GetSpan(int sizeHint = 0)
    {
        CheckIfDisposed();

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

            var previousBuffer = oldBuffer.AsSpan(0, _index);
            previousBuffer.CopyTo(_rentedBuffer);
            previousBuffer.Clear();
            ArrayPool<T>.Shared.Return(oldBuffer);
        }

        Debug.Assert(_rentedBuffer.Length - _index > 0);
        Debug.Assert(_rentedBuffer.Length - _index >= sizeHint);
    }

    private static void ThrowInvalidOperationException(int capacity)
    {
        throw new InvalidOperationException($"Cannot advance past the end of the buffer, which has a size of {capacity}.");
    }
}