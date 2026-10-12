using System.Buffers;
using Eryri.Buffers.Extensions;
using FluentAssertions;

namespace Eryri.Buffers.Tests.MemoryStreamTests;

[TestFixtureSource(nameof(Types)), Parallelizable(ParallelScope.All)]
internal class StreamTests(Type type)
{
    public static IEnumerable<TestFixtureData> Types()
    {
        yield return new TestFixtureData(typeof(MemoryPoolStream));
        yield return new TestFixtureData(typeof(SequenceStream));
    }

    [Test]
    public void Constructor_ShouldCreateEmptyStream()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;
        stream.Length.Should().Be(0);
        stream.Position.Should().Be(0);
        stream.CanRead.Should().BeTrue();
        stream.CanWrite.Should().BeTrue();
        stream.CanSeek.Should().BeTrue();
    }

    [Test]
    public void Write_ShouldAdvancePositionAndLength()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;
        byte[] data = [1, 2, 3, 4, 5];

        stream.Write(data);

        stream.Position.Should().Be(5);
        stream.Length.Should().Be(5);
    }

    [Test]
    public void Read_ShouldReturnWrittenData()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;
        byte[] expected = [1, 2, 3, 4, 5];
        stream.Write(expected);
        stream.Position = 0;

        byte[] actual = new byte[expected.Length];
        var bytesRead = stream.Read(actual);

        bytesRead.Should().Be(expected.Length);
        actual.Should().Equal(expected);
        stream.Position.Should().Be(expected.Length);
    }

    [Test]
    public void ReadAtEnd_ShouldReturnZero()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;
        stream.WriteByte(42);

        var result = stream.ReadByte();

        result.Should().Be(-1);
    }

    [Test]
    public void ReadByte_ShouldReturnWrittenByte()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;
        stream.WriteByte(42);
        stream.Position = 0;

        stream.ReadByte().Should().Be(42);
        stream.Position.Should().Be(1);
    }

    [Test]
    public void WriteByte_ShouldWriteSingleByte()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;
        stream.WriteByte(42);

        stream.Position.Should().Be(1);
        stream.Length.Should().Be(1);

        stream.Position = 0;
        stream.ReadByte().Should().Be(42);
    }

    [Test]
    public void Write_ShouldOverwriteExistingData()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;
        stream.Write(new byte[] { 1, 2, 3, 4, 5 });
        stream.Position = 1;

        stream.Write(new byte[] { 9, 8 });

        stream.Position.Should().Be(3);
        stream.Length.Should().Be(5);

        stream.Position = 0;
        stream.ToArray().Should().Equal(1, 9, 8, 4, 5);
    }

    [Test]
    public void WriteBeyondLength_ShouldZeroFillGap()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;
        stream.Write(new byte[] { 1, 2, 3 });
        stream.Position = 6;

        stream.WriteByte(9);

        stream.Length.Should().Be(7);
        stream.Position = 0;

        stream.ToArray().Should().Equal(1, 2, 3, 0, 0, 0, 9);
    }

    [Test]
    public void SeekBeyondEnd_ShouldNotChangeLength()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;
        stream.Position = 100;

        stream.Position.Should().Be(100);
        stream.Length.Should().Be(0);
    }

    [Test]
    public void Seek_ShouldSupportAllOrigins()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;
        stream.Write(new byte[10]);

        stream.Seek(3, SeekOrigin.Begin).Should().Be(3);
        stream.Seek(2, SeekOrigin.Current).Should().Be(5);
        stream.Seek(-2, SeekOrigin.End).Should().Be(8);
    }

    [Test]
    public void SeekToNegativePosition_ShouldThrow()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;
        var act = () => stream.Seek(-1, SeekOrigin.Begin);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void PositionNegative_ShouldThrow()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;
        var act = () => stream.Position = -1;

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void SetLength_ShouldExtendAndZeroFill()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;
        stream.Write(new byte[] { 1, 2, 3 });

        stream.SetLength(6);

        stream.Length.Should().Be(6);
        stream.Position = 0;

        stream.ToArray().Should().Equal(1, 2, 3, 0, 0, 0);
    }

    [Test]
    public void SetLength_ShouldTruncateStream()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;
        stream.Write(new byte[] { 1, 2, 3, 4, 5 });

        stream.SetLength(3);

        stream.Length.Should().Be(3);
        stream.Position.Should().Be(3);

        stream.Position = 0;
        stream.ToArray().Should().Equal(1, 2, 3);
    }

    [Test]
    public void SetLength_ShouldClampPosition()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;
        stream.Write(new byte[10]);
        stream.Position = 8;

        stream.SetLength(4);

        stream.Position.Should().Be(4);
    }

    [Test]
    public void SetLength_ShouldZeroPreviouslyWrittenDataWhenExtendedAgain()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;
        stream.Write(new byte[] { 1, 2, 3, 4, 5 });
        stream.SetLength(2);
        stream.SetLength(5);

        stream.Position = 0;

        stream.ToArray().Should().Equal(1, 2, 0, 0, 0);
    }

    [Test]
    public void Write_ShouldGrowCapacityForLargeData()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;
        var data = Enumerable.Range(0, 100_000)
            .Select(i => (byte)(i % 256))
            .ToArray();

        stream.Write(data);

        stream.Length.Should().Be(data.Length);

        stream.Position = 0;
        stream.ToArray().Should().Equal(data);
    }

    [Test]
    public void ReadAndWrite_ShouldCrossSegmentBoundaries()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;
        var data = Enumerable.Range(0, 20_000)
            .Select(i => (byte)(i % 256))
            .ToArray();

        stream.Write(data);
        stream.Position = 100;

        var actual = new byte[10_000];
        stream.ReadExactly(actual);

        actual.Should().Equal(data.Skip(100).Take(10_000));
    }

    [Test]
    public void Flush_ShouldNotThrow()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;
        var act = () => stream.Flush();

        act.Should().NotThrow();
    }

    [Test]
    public void DisposedStream_ShouldRejectOperations()
    {
        var stream = (Activator.CreateInstance(type) as Stream)!;
        stream.Dispose();

        stream.CanRead.Should().BeFalse();
        stream.CanWrite.Should().BeFalse();
        stream.CanSeek.Should().BeFalse();

        var read = () => stream.Read(new byte[1]);
        var write = () => stream.WriteByte(1);
        var seek = () => stream.Seek(0, SeekOrigin.Begin);

        read.Should().Throw<ObjectDisposedException>();
        write.Should().Throw<ObjectDisposedException>();
        seek.Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void Dispose_ShouldBeIdempotent()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;
        var act = () =>
        {
            stream.Dispose();
            stream.Dispose();
        };

        act.Should().NotThrow();
    }

    [Test]
    public void Write_ShouldCrossMultipleSegments()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;

        var data = Enumerable.Range(0, 100_000)
            .Select(i => (byte)(i % 251))
            .ToArray();

        stream.Write(data);

        stream.Length.Should().Be(data.Length);

        stream.Position = 0;
        stream.ToArray().Should().Equal(data);
    }

    [Test]
    public void Read_ShouldCrossMultipleSegmentsFromNonZeroPosition()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;

        var data = Enumerable.Range(0, 100_000)
            .Select(i => (byte)(i % 251))
            .ToArray();

        stream.Write(data);
        stream.Position = 12_345;

        var actual = new byte[50_000];
        var bytesRead = stream.Read(actual);

        bytesRead.Should().Be(actual.Length);
        actual.Should().Equal(data.Skip(12_345).Take(actual.Length));
        stream.Position.Should().Be(12_345 + actual.Length);
    }

    [Test]
    public void Write_ShouldOverwriteAcrossMultipleSegments()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;

        var original = Enumerable.Repeat((byte)1, 100_000).ToArray();
        var replacement = Enumerable.Repeat((byte)2, 20_000).ToArray();

        stream.Write(original);
        stream.Position = 15_000;
        stream.Write(replacement);

        stream.Length.Should().Be(original.Length);

        stream.Position = 0;
        var actual = stream.ToArray();

        actual.Take(15_000).Should().OnlyContain(b => b == 1);
        actual.Skip(15_000).Take(20_000).Should().OnlyContain(b => b == 2);
        actual.Skip(35_000).Should().OnlyContain(b => b == 1);
    }

    [Test]
    public void SetLength_ShouldExtendAcrossMultipleSegmentsAndZeroFill()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;

        stream.Write(new byte[] { 1, 2, 3 });

        stream.SetLength(50_000);

        stream.Length.Should().Be(50_000);

        stream.Position = 0;
        var actual = stream.ToArray();

        actual.Take(3).Should().Equal(1, 2, 3);
        actual.Skip(3).Should().OnlyContain(b => b == 0);
    }

    [Test]
    public void SetLength_ShouldTruncateAcrossMultipleSegments()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;

        var data = Enumerable.Range(0, 100_000)
            .Select(i => (byte)(i % 251))
            .ToArray();

        stream.Write(data);
        stream.SetLength(10_000);

        stream.Length.Should().Be(10_000);
        stream.Position.Should().Be(10_000);

        stream.Position = 0;
        stream.ToArray().Should().Equal(data.Take(10_000));
    }

    [Test]
    public void SetLength_ShouldTruncateAndReextendAcrossMultipleSegments()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;

        var data = Enumerable.Repeat((byte)0xFF, 100_000).ToArray();
        stream.Write(data);

        stream.SetLength(10_000);
        stream.SetLength(80_000);

        stream.Length.Should().Be(80_000);

        stream.Position = 0;
        var actual = stream.ToArray();

        actual.Take(10_000).Should().OnlyContain(b => b == 0xFF);
        actual.Skip(10_000).Should().OnlyContain(b => b == 0);
    }

    [Test]
    public void WriteBeyondEnd_ShouldZeroFillAcrossMultipleSegments()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;

        stream.Write(new byte[] { 1, 2, 3 });
        stream.Position = 50_000;
        stream.Write(new byte[] { 9, 8, 7 });

        stream.Length.Should().Be(50_003);

        stream.Position = 0;
        var actual = stream.ToArray();

        actual.Take(3).Should().Equal(1, 2, 3);
        actual.Skip(3).Take(49_997).Should().OnlyContain(b => b == 0);
        actual.Skip(50_000).Should().Equal(9, 8, 7);
    }

    [Test]
    public void ReadByte_ShouldReadCorrectlyAcrossSegmentBoundaries()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;

        var data = Enumerable.Range(0, 100_000)
            .Select(i => (byte)(i % 251))
            .ToArray();

        stream.Write(data);
        stream.Position = 0;

        for (var i = 0; i < data.Length; i++)
        {
            stream.ReadByte().Should().Be(data[i], $"at position {i}");
        }

        stream.ReadByte().Should().Be(-1);
    }

    [Test]
    public void WriteByte_ShouldWriteCorrectlyAcrossSegmentBoundaries()
    {
        using var stream = (Activator.CreateInstance(type) as Stream)!;

        const int length = 50_000;

        for (var i = 0; i < length; i++)
        {
            stream.WriteByte((byte)(i % 251));
        }

        stream.Length.Should().Be(length);

        stream.Position = 0;

        for (var i = 0; i < length; i++)
        {
            stream.ReadByte().Should().Be((byte)(i % 251), $"at position {i}");
        }
    }
}
