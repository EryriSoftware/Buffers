using System.Buffers;
using System.Diagnostics;

namespace Eryri.Buffers;

/// <summary>
/// A seekable, array-backed stream using <see cref="ArrayPool{T}"/> for storage.
/// </summary>
/// <remarks>
/// Supports random-access reads and writes. Seeking beyond the end does not
/// change the stream length; writing beyond the end fills the gap with zeroes.
/// The maximum stream length is <see cref="int.MaxValue"/>.
/// This stream is not thread-safe.
/// </remarks>
public class MemoryPoolStream : Stream
{
    private const int MinimumBufferSize = 256;

    private byte[] _rentedBuffer;
    private readonly bool _clearOnReturn;
    private int _length;
    private int _position;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryPoolStream"/> class.
    /// </summary>
    public MemoryPoolStream() : this(MinimumBufferSize, false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryPoolStream"/> class.
    /// </summary>
    /// <param name="clearOnReturn">Decides if the rented arrays are cleared when they're returned to the pool.</param>
    public MemoryPoolStream(bool clearOnReturn = false) : this(MinimumBufferSize, clearOnReturn)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryPoolStream"/> class with the specified initial capacity.
    /// </summary>
    /// <param name="initialCapacity">The minimum initial capacity.</param>
    /// <param name="clearOnReturn">Decides if the rented arrays are cleared when they're returned to the pool.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="initialCapacity"/> is not positive.
    /// </exception>
    public MemoryPoolStream(int initialCapacity, bool clearOnReturn = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialCapacity);
        _rentedBuffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
        _clearOnReturn = clearOnReturn;
    }

    /// <inheritdoc />
    public override bool CanRead => !_isDisposed;

    /// <inheritdoc />
    public override bool CanSeek => !_isDisposed;

    /// <inheritdoc />
    public override bool CanWrite => !_isDisposed;

    /// <summary>
    /// Gets or sets the capacity (number of bytes allocated) for this stream.
    /// The capacity cannot be set to a value less than the current length
    /// of the stream.
    /// </summary>
    public int Capacity
    {
        get
        {
            ThrowIfDisposed();
            return _rentedBuffer.Length;
        }
        set
        {
            ThrowIfDisposed();
            ArgumentOutOfRangeException.ThrowIfLessThan(value, _length);
            EnsureCapacity(value);
        }
    }

    /// <inheritdoc />
    public override long Length
    {
        get
        {
            ThrowIfDisposed();
            return _length;
        }
    }

    /// <inheritdoc />
    public override long Position
    {
        get
        {
            ThrowIfDisposed();
            return _position;
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, int.MaxValue);
            ThrowIfDisposed();

            _position = (int)value;
        }
    }

    /// <inheritdoc />
    public override void Flush() => ThrowIfDisposed();

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        return Read(buffer.AsSpan(offset, count));
    }

    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        ThrowIfDisposed();

        int count = Math.Clamp(_length - _position, 0, buffer.Length);

        _rentedBuffer.AsSpan(_position, count).CopyTo(buffer);
        _position += count;

        return count;
    }

    /// <inheritdoc />
    public override int ReadByte()
    {
        ThrowIfDisposed();

        return _position < _length
            ? _rentedBuffer[_position++]
            : -1;
    }

    /// <summary>
    /// Gets the data written to the underlying buffer so far, as a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    public ReadOnlySpan<byte> WrittenSpan
    {
        get
        {
            ThrowIfDisposed();
            return _rentedBuffer.AsSpan(0, _length);
        }
    }

    /// <summary>
    /// Gets the data written to the underlying buffer so far, as a <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    public ReadOnlyMemory<byte> WrittenMemory
    {
        get
        {
            ThrowIfDisposed();
            return _rentedBuffer.AsMemory().Slice(0, _length);
        }
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfDisposed();

        long position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => checked(_position + offset),
            SeekOrigin.End => checked(_length + offset),
            _ => throw new ArgumentException("Invalid seek origin.", nameof(origin))
        };

        ArgumentOutOfRangeException.ThrowIfNegative(position);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(position, int.MaxValue);

        _position = (int)position;
        return _position;
    }

    /// <inheritdoc />
    public override void SetLength(long value)
    {
        ThrowIfDisposed();
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, int.MaxValue);

        int length = (int)value;
        EnsureCapacity(length);
        _rentedBuffer.AsSpan(Math.Min(length, _length), Math.Abs(length - _length)).Clear();
        _length = length;
        _position = Math.Min(_position, _length);
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        Write(buffer.AsSpan(offset, count));
    }

    /// <inheritdoc />
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ThrowIfDisposed();
        var end = PrepareWrite(buffer.Length);

        buffer.CopyTo(_rentedBuffer.AsSpan(_position, buffer.Length));

        _position = end;
        _length = Math.Max(_length, end);
    }

    /// <inheritdoc />
    public override void WriteByte(byte value)
    {
        ThrowIfDisposed();
        var end = PrepareWrite(1);

        _rentedBuffer[_position] = value;

        _position = end;
        _length = Math.Max(_length, end);
    }

    private int PrepareWrite(int count)
    {
        int end = checked(_position + count);
        EnsureCapacity(end);

        if (_position > _length)
        {
            _rentedBuffer.AsSpan(_length, _position - _length).Clear();
        }

        return end;
    }

    private void EnsureCapacity(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, int.MaxValue);

        if (value > _rentedBuffer.Length)
        {
            int doubleLength = _rentedBuffer.Length <= int.MaxValue / 2 ? _rentedBuffer.Length * 2 : int.MaxValue;
            int capacity = Math.Max(value, doubleLength);

            var oldBuffer = _rentedBuffer;

            _rentedBuffer = ArrayPool<byte>.Shared.Rent(capacity);

            Debug.Assert(oldBuffer.Length >= _length);
            Debug.Assert(_rentedBuffer.Length >= _length);

            oldBuffer.AsSpan(0, _length).CopyTo(_rentedBuffer);
            ArrayPool<byte>.Shared.Return(oldBuffer, _clearOnReturn);
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_isDisposed)
        {
            _isDisposed = true;
            ArrayPool<byte>.Shared.Return(_rentedBuffer, _clearOnReturn);
            _rentedBuffer = Array.Empty<byte>();
        }

        base.Dispose(disposing);
    }
}