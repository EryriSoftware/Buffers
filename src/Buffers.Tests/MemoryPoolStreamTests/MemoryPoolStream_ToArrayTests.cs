using Eryri.Buffers.Extensions;
using FluentAssertions;

namespace Eryri.Buffers.Tests.MemoryPoolStreamTests;

[TestFixture, Parallelizable(ParallelScope.All)]
internal class MemoryPoolStream_ToArrayTests
{
    [TestCaseSource(nameof(GetArraysVariedBySize))]
    public void ToArray_ZeroOffset(byte[] array)
    {
        var stream = new MemoryPoolStream();
        stream.Write(array);
        var newArray = stream.ToArray();

        array.Length.Should().Be(newArray.Length);
        array.Should().BeEquivalentTo(newArray);
    }

    [TestCaseSource(nameof(GetArraysVariedBySize))]
    public void ToArray_Offset(byte[] array)
    {
        int index = 0;
        int count = array.Length;

        if (count > 3)
        {
            // Trim some off each end
            index = 1;
            count -= 3;
        }

        using var stream = new MemoryPoolStream();
        stream.Write(array.AsSpan(index, count));
        var newArray = stream.ToArray();

        count.Should().Be(newArray.Length);
        array.AsSpan(index, count).SequenceEqual(newArray).Should().BeTrue();
    }

    private static byte[] GetRandomBytes(int count)
    {
        byte[] bytes = new byte[count];
        Random.Shared.NextBytes(bytes);
        return bytes;
    }

    public static IEnumerable<object[]> GetArraysVariedBySize()
    {
        yield return new object[] { GetRandomBytes(0) };
        yield return new object[] { GetRandomBytes(1) };
        yield return new object[] { GetRandomBytes(2) };
        yield return new object[] { GetRandomBytes(256) };
        yield return new object[] { GetRandomBytes(512) };
        yield return new object[] { GetRandomBytes(1024) };
        yield return new object[] { GetRandomBytes(2047) };
        yield return new object[] { GetRandomBytes(2048) };
        yield return new object[] { GetRandomBytes(2049) };
        yield return new object[] { GetRandomBytes(2100) };
    }
}
