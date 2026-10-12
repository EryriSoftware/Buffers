using System.Buffers;

namespace Eryri.Buffers;

/// <summary>
/// A seekable stream backed by a sequence of rented arrays using <see cref="ArrayPool{T}"/>.
/// </summary>
/// <remarks>
/// Supports random-access reads and writes without relocating existing data.
/// Seeking beyond the end does not change the stream length; writing beyond
/// the end fills the gap with zeroes.
/// The stream is not thread-safe.
/// </remarks>
public sealed class SequenceStream : Stream
{
    private sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public byte[] Buffer { get; }
        public Segment? NextSegment { get; private set; }
        public long End => RunningIndex + Memory.Length;

        public Segment(byte[] buffer, long runningIndex)
        {
            Buffer = buffer;
            Memory = buffer;
            RunningIndex = runningIndex;
        }

        public Segment Append(byte[] buffer)
        {
            var next = new Segment(buffer, End);
            Next = NextSegment = next;
            return next;
        }

        public Segment SeekTo(long position)
        {
            var result = this;

            while (position >= result.End)
            {
                result = result.NextSegment ?? throw new ArgumentOutOfRangeException(nameof(position));
            }

            return result;
        }
    }

    private const int MaxBufferSize = 4 << 20; // 4 MiB
    private const int MinimumBufferSize = 256;
    private readonly bool _clearOnReturn;

    private Segment _first;
    private Segment _last;

    private long _position;
    private long _length;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SequenceStream"/> class,
    /// in which data can be written to, with the default initial capacity.
    /// </summary>
    public SequenceStream() : this(MinimumBufferSize, false) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SequenceStream"/> class,
    /// in which data can be written to, with the default initial capacity.
    /// </summary>
    /// <param name="clearOnReturn">Decides if the rented arrays are cleared when they're returned to the pool.</param>
    public SequenceStream(bool clearOnReturn = false) : this(MinimumBufferSize, clearOnReturn) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SequenceStream"/> class.
    /// </summary>
    /// <param name="initialCapacity">The minimum capacity with which to initialize the underlying buffer.</param>
    /// <param name="clearOnReturn">Decides if the rented arrays are cleared when they're returned to the pool.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="initialCapacity"/> is not positive (i.e. less than or equal to 0).
    /// </exception>
    public SequenceStream(int initialCapacity, bool clearOnReturn = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialCapacity);

        _clearOnReturn = clearOnReturn;
        _first = _last = new Segment(ArrayPool<byte>.Shared.Rent(initialCapacity), 0);
    }

    public override bool CanRead => !_disposed;
    public override bool CanSeek => !_disposed;
    public override bool CanWrite => !_disposed;

    public override long Length
    {
        get
        {
            ThrowIfDisposed();
            return _length;
        }
    }

    public override long Position
    {
        get
        {
            ThrowIfDisposed();
            return _position;
        }
        set
        {
            ThrowIfDisposed();
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _position = value;
        }
    }

    /// <summary>
    /// Gets or sets the capacity for this stream.
    /// The capacity cannot be set to a value less than the current length
    /// of the stream.
    /// </summary>
    public long Capacity
    {
        get
        {
            ThrowIfDisposed();
            return _last.End;
        }
        set
        {
            ThrowIfDisposed();
            ArgumentOutOfRangeException.ThrowIfLessThan(value, _length);
            EnsureCapacity(value);
        }
    }

    /// <summary>
    /// Gets the written data as a zero-copy sequence.
    /// The sequence is valid only while the stream remains undisposed
    /// and its underlying buffers are not modified.
    /// </summary>
    public ReadOnlySequence<byte> WrittenSequence
    {
        get
        {
            ThrowIfDisposed();

            if (_length == 0)
            {
                return ReadOnlySequence<byte>.Empty;
            }

            var last = _first.SeekTo(_length - 1);

            return new ReadOnlySequence<byte>(
                _first!,
                0,
                last,
                (int)(_length - last.RunningIndex));
        }
    }

    private void EnsureCapacity(long value)
    {
        while (Capacity < value)
        {
            var currentCapacity = Capacity;
            var doubledCapacity = currentCapacity <= MaxBufferSize / 2
                ? currentCapacity * 2
                : MaxBufferSize;

            var requiredCapacity = value - currentCapacity;
            var segmentCapacity = (int)Math.Min(
                MaxBufferSize,
                Math.Max(requiredCapacity, doubledCapacity));

            _last = _last.Append(ArrayPool<byte>.Shared.Rent(segmentCapacity));
        }
    }

    private void ClearRange(long start, long end)
    {
        var segment = _first;
        while (start < end)
        {
            segment = segment.SeekTo(start);
            var offset = (int)(start - segment.RunningIndex);
            var length = (int)Math.Min(
                end - start,
                segment.Buffer.Length - offset);

            segment.Buffer.AsSpan(offset, length).Clear();
            start += length;
        }
    }

    public override int Read(Span<byte> buffer)
    {
        ThrowIfDisposed();

        var count = (int)Math.Min(
            buffer.Length,
            Math.Max(0L, _length - _position));

        var remaining = count;
        var destinationOffset = 0;
        var segment = _first;

        while (remaining > 0)
        {
            segment = segment.SeekTo(_position);
            var offset = (int)(_position - segment.RunningIndex);
            var length = Math.Min(
                remaining,
                segment.Buffer.Length - offset);

            segment.Buffer.AsSpan(offset, length)
                .CopyTo(buffer[destinationOffset..]);

            _position += length;
            destinationOffset += length;
            remaining -= length;
        }

        return count;
    }

    public override int ReadByte()
    {
        ThrowIfDisposed();

        if (_position >= _length)
        {
            return -1;
        }

        var segment = _first.SeekTo(_position);
        var offset = (int)(_position - segment.RunningIndex);

        _position++;
        return segment.Buffer[offset];
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ThrowIfDisposed();

        if (buffer.IsEmpty)
        {
            return;
        }

        var end = checked(_position + buffer.Length);
        EnsureCapacity(end);
        ClearRange(_length, _position);

        var segment = _first.SeekTo(_position);
        var offset = (int)(_position - segment.RunningIndex);

        if (buffer.Length <= segment.Buffer.Length - offset)
        {
            buffer.CopyTo(segment.Buffer.AsSpan(offset));
        }
        else
        {
            var remaining = buffer.Length;
            var sourceOffset = 0;

            while (remaining > 0)
            {
                var length = Math.Min(
                    remaining,
                    segment.Buffer.Length - offset);

                buffer.Slice(sourceOffset, length)
                    .CopyTo(segment.Buffer.AsSpan(offset, length));

                remaining -= length;
                sourceOffset += length;

                if (remaining > 0)
                {
                    segment = segment.NextSegment!;
                    offset = 0;
                }
            }
        }

        _position = end;
        _length = Math.Max(_length, end);
    }

    public override void WriteByte(byte value)
    {
        ThrowIfDisposed();

        var end = checked(_position + 1);
        EnsureCapacity(end);
        ClearRange(_length, _position);

        var segment = _first.SeekTo(_position);
        segment.Buffer[(int)(_position - segment.RunningIndex)] = value;

        _position = end;
        _length = Math.Max(_length, end);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfDisposed();

        var position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => checked(_position + offset),
            SeekOrigin.End => checked(_length + offset),
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

        ArgumentOutOfRangeException.ThrowIfNegative(position);

        return _position = position;
    }

    public override void SetLength(long value)
    {
        ThrowIfDisposed();
        ArgumentOutOfRangeException.ThrowIfNegative(value);

        EnsureCapacity(value);
        ClearRange(_length, value);

        _length = value;
        _position = Math.Min(_position, value);
    }

    public override void Flush() => ThrowIfDisposed();

    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        return Read(buffer.AsSpan(offset, count));
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        Write(buffer.AsSpan(offset, count));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;

            for (var segment = _first; segment is not null; segment = segment.NextSegment)
            {
                ArrayPool<byte>.Shared.Return(segment.Buffer, _clearOnReturn);
            }

            _length = _position = 0;
        }

        base.Dispose(disposing);
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}