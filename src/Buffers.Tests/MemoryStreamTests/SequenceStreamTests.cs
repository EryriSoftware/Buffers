using System.Buffers;
using Eryri.Buffers.Extensions;
using FluentAssertions;

namespace Eryri.Buffers.Tests.MemoryStreamTests;

[TestFixture, Parallelizable(ParallelScope.All)]
internal class SequenceStreamTests
{
    [Test]
    public void WrittenSequence_ShouldExposeWrittenData()
    {
        using var stream = new SequenceStream();
        byte[] expected = [1, 2, 3, 4, 5];

        stream.Write(expected);

        stream.WrittenSequence.ToArray().Should().Equal(expected);
    }

    [Test]
    public void WrittenSequence_ShouldBeEmptyForEmptyStream()
    {
        using var stream = new SequenceStream();
        stream.WrittenSequence.IsEmpty.Should().BeTrue();
    }

    [Test]
    public void WrittenSequence_ShouldExcludeUnusedCapacity()
    {
        using var stream = new SequenceStream();
        stream.Write(new byte[] { 1, 2, 3 });

        stream.WrittenSequence.Length.Should().Be(3);
    }

    [Test]
    public void WrittenSequence_ShouldContainDataAcrossMultipleSegments()
    {
        using var stream = new SequenceStream();

        var data = Enumerable.Range(0, 100_000)
            .Select(i => (byte)(i % 251))
            .ToArray();

        stream.Write(data);

        var sequence = stream.WrittenSequence;

        sequence.Length.Should().Be(data.Length);
        sequence.ToArray().Should().Equal(data);
    }
}
