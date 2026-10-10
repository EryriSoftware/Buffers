using FluentAssertions;

namespace Eryri.Buffers.Tests.MemoryPoolStreamTests;

[TestFixture, Parallelizable(ParallelScope.All)]
internal class MemoryPoolStreamTests
{
    [Test]
    public static void Span_Returns_Written_Data()
    {
        using (var ms = new MemoryPoolStream())
        {
            Span<byte> bytArr = new byte[] { byte.MinValue, byte.MaxValue, 1, 2, 3, 4, 5, 6, 128, 250 };

            // [] Write to FileStream, check the filestream
            ms.Write(bytArr);
            var bytArrRet = ms.WrittenSpan;

            bytArrRet.Length.Should().Be(bytArr.Length);
            for (int i = 0; i < bytArr.Length; i++)
            {
                bytArr[i].Should().Be(bytArrRet[i]);
            }
        }
    }

    [Test]
    public static void Memory_Returns_Written_Data()
    {
        using (var ms = new MemoryPoolStream())
        {
            Span<byte> bytArr = new byte[] { byte.MinValue, byte.MaxValue, 1, 2, 3, 4, 5, 6, 128, 250 };

            // [] Write to FileStream, check the filestream
            ms.Write(bytArr);
            var bytArrRet = ms.WrittenMemory;

            bytArrRet.Length.Should().Be(bytArr.Length);
            for (int i = 0; i < bytArr.Length; i++)
            {
                bytArr[i].Should().Be(bytArrRet.Span[i]);
            }
        }
    }
}
