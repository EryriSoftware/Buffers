using System.Buffers;
using FluentAssertions;

namespace Eryri.Buffers.Tests;

public class PooledArrayBufferWriterTests_Byte : PooledArrayBufferWriterTests<byte>
{
    protected override void WriteData(IBufferWriter<byte> bufferWriter, int numBytes)
    {
        Span<byte> outputSpan = bufferWriter.GetSpan(numBytes);
        outputSpan.Length.Should().BeGreaterThanOrEqualTo(numBytes);
        var random = new Random(42);

        var data = new byte[numBytes];
        random.NextBytes(data);
        data.CopyTo(outputSpan);

        bufferWriter.Advance(numBytes);
    }

    [Theory]
    [TestCase(true)]
    [TestCase(false)]
    public void WriteAndCopyToStream(bool clearContent)
    {
        using var output = new PooledArrayBufferWriter<byte>();
        WriteData(output, 100);

        using MemoryStream memStream = new (100);

        output.WrittenCount.Should().Be(100);

        ReadOnlySpan<byte> outputSpan = output.WrittenMemory.ToArray();

        ReadOnlyMemory<byte> transientMemory = output.WrittenMemory;
        ReadOnlySpan<byte> transientSpan = output.WrittenSpan;

        transientSpan.SequenceEqual(transientMemory.Span).Should().BeTrue();

        transientSpan[0].Should().NotBe(0);
        byte expectedFirstByte = transientSpan[0];

        memStream.Write(transientSpan.ToArray(), 0, transientSpan.Length);

        if (clearContent)
        {
            expectedFirstByte = 0;
            output.Clear();
        }
        else
        {
            output.ResetWrittenCount();
        }

        transientSpan[0].Should().Be(expectedFirstByte);
        transientMemory.Span[0].Should().Be(expectedFirstByte);

        output.WrittenCount.Should().Be(0);
        byte[] streamOutput = memStream.ToArray();

        ReadOnlyMemory<byte>.Empty.Span.SequenceEqual(output.WrittenMemory.Span).Should().BeTrue();
        ReadOnlySpan<byte>.Empty.SequenceEqual(output.WrittenMemory.Span).Should().BeTrue();
        output.WrittenSpan.SequenceEqual(output.WrittenMemory.Span).Should().BeTrue();

        streamOutput.Length.Should().Be(outputSpan.Length);
        outputSpan.SequenceEqual(streamOutput).Should().BeTrue();
    }

    [Theory]
    [TestCase(true)]
    [TestCase(false)]
    public async Task WriteAndCopyToStreamAsync(bool clearContent)
    {
        using var output = new PooledArrayBufferWriter<byte>();
        WriteData(output, 100);

        using MemoryStream memStream = new (100);

        output.WrittenCount.Should().Be(100);

        ReadOnlyMemory<byte> outputMemory = output.WrittenMemory.ToArray();

        ReadOnlyMemory<byte> transient = output.WrittenMemory;

        transient.Span[0].Should().NotBe(0);
        byte expectedFirstByte = transient.Span[0];

        await memStream.WriteAsync(transient.ToArray(), 0, transient.Length);

        if (clearContent)
        {
            expectedFirstByte = 0;
            output.Clear();
        }
        else
        {
            output.ResetWrittenCount();
        }

        transient.Span[0].Should().Be(expectedFirstByte);

        output.WrittenCount.Should().Be(0);
        byte[] streamOutput = memStream.ToArray();

        ReadOnlyMemory<byte>.Empty.Span.SequenceEqual(output.WrittenMemory.Span).Should().BeTrue();
        ReadOnlySpan<byte>.Empty.SequenceEqual(output.WrittenMemory.Span).Should().BeTrue();

        streamOutput.Length.Should().Be(outputMemory.Length);
        outputMemory.Span.SequenceEqual(streamOutput).Should().BeTrue();
    }
}
