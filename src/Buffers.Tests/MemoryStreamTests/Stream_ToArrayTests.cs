using Eryri.Buffers.Extensions;
using FluentAssertions;

namespace Eryri.Buffers.Tests.MemoryStreamTests;

[TestFixture, Parallelizable(ParallelScope.All)]
internal class Stream_ToArrayTests
{
    [TestCaseSource(nameof(GetArraysVariedBySize))]
    public void ToArray_ZeroOffset(byte[] array)
    {
        using var stream = new MemoryPoolStream();
        stream.Write(array);
        var newArray = stream.ToArray();

        array.Length.Should().Be(newArray.Length);
        array.Should().BeEquivalentTo(newArray);
    }

    [TestCaseSource(nameof(GetArraysVariedBySize))]
    public void ToArray_Offset(byte[] array)
    {
        using var stream = new MemoryPoolStream();
        int index = 0;
        int count = array.Length;

        if (count > 3)
        {
            // Trim some off each end
            index = 1;
            count -= 3;
        }

        stream.Write(array.AsSpan(index, count));
        var newArray = stream.ToArray();

        count.Should().Be(newArray.Length);
        array.AsSpan(index, count).SequenceEqual(newArray).Should().BeTrue();
    }

    private static byte[] GetRandomBytes(int count)
    {
        byte[] bytes = new byte[count];
        new Random(12345 + count).NextBytes(bytes);
        return bytes;
    }

    public static IEnumerable<TestCaseData> GetArraysVariedBySize()
    {
        yield return new TestCaseData(GetRandomBytes(0));
        yield return new TestCaseData(GetRandomBytes(1));
        yield return new TestCaseData(GetRandomBytes(2));
        yield return new TestCaseData(GetRandomBytes(256));
        yield return new TestCaseData(GetRandomBytes(512));
        yield return new TestCaseData(GetRandomBytes(1024));
        yield return new TestCaseData(GetRandomBytes(2047));
        yield return new TestCaseData(GetRandomBytes(2048));
        yield return new TestCaseData(GetRandomBytes(2049));
        yield return new TestCaseData(GetRandomBytes(2100));
    }
}
