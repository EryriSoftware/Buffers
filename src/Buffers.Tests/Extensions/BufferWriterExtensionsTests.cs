using System.Buffers;
using AutoFixture;
using Eryri.Buffers.Extensions;
using FluentAssertions;

namespace Eryri.Buffers.Tests.Extensions;

[TestFixture, Parallelizable(ParallelScope.All)]
internal class BufferWriterExtensionsTests
{
    private Fixture fixture = new Fixture();
    private const int BufferSize = 1 << 10; // 1 KiB

    [Test]
    public void Can_Write_And_Read_Byte()
    {
        // Arrange
        var expected = fixture.Create<byte>();
        using var writer = new PooledArrayBufferWriter<byte>(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        writer.Write(expected);
        var read = writer.WrittenSpan.ReadByte(out var actual);

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        read.Should().Be(writer.WrittenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_String()
    {
        // Arrange
        var expected = fixture.Create<string>();
        using var writer = new PooledArrayBufferWriter<byte>(BufferSize);

        // Act
        writer.Write(expected);
        var read = writer.WrittenSpan.ReadString(out var actual);

        // Assert
        read.Should().Be(writer.WrittenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_Char()
    {
        // Arrange
        var expected = "😀"[0];
        using var writer = new PooledArrayBufferWriter<byte>(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        writer.Write(expected);
        var read = writer.WrittenSpan.ReadChar(out var actual);

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        read.Should().Be(writer.WrittenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_Guid()
    {
        // Arrange
        var expected = fixture.Create<Guid>();
        using var writer = new PooledArrayBufferWriter<byte>(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        writer.Write(expected);
        var read = writer.WrittenSpan.ReadGuid(out var actual);

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        read.Should().Be(writer.WrittenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_Bool()
    {
        // Arrange
        var expected = fixture.Create<bool>();
        using var writer = new PooledArrayBufferWriter<byte>(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        writer.Write(expected);
        var read = writer.WrittenSpan.ReadBoolean(out var actual);

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        read.Should().Be(writer.WrittenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_Short()
    {
        // Arrange
        var expected = fixture.Create<short>();
        using var writer = new PooledArrayBufferWriter<byte>(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        writer.Write(expected);
        var read = writer.WrittenSpan.ReadShort(out var actual);

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        read.Should().Be(writer.WrittenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_UShort()
    {
        // Arrange
        var expected = fixture.Create<ushort>();
        using var writer = new PooledArrayBufferWriter<byte>(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        writer.Write(expected);
        var read = writer.WrittenSpan.ReadUShort(out var actual);

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        read.Should().Be(writer.WrittenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_Int()
    {
        // Arrange
        var expected = fixture.Create<int>();
        using var writer = new PooledArrayBufferWriter<byte>(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        writer.Write(expected);
        var read = writer.WrittenSpan.ReadInt(out var actual);

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        read.Should().Be(writer.WrittenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_UInt()
    {
        // Arrange
        var expected = fixture.Create<uint>();
        using var writer = new PooledArrayBufferWriter<byte>(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        writer.Write(expected);
        var read = writer.WrittenSpan.ReadUInt(out var actual);

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        read.Should().Be(writer.WrittenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_Long()
    {
        // Arrange
        var expected = fixture.Create<long>();
        using var writer = new PooledArrayBufferWriter<byte>(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        writer.Write(expected);
        var read = writer.WrittenSpan.ReadLong(out var actual);

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        read.Should().Be(writer.WrittenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_ULong()
    {
        // Arrange
        var expected = fixture.Create<ulong>();
        using var writer = new PooledArrayBufferWriter<byte>(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        writer.Write(expected);
        var read = writer.WrittenSpan.ReadULong(out var actual);

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        read.Should().Be(writer.WrittenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_Half()
    {
        // Arrange
        var expected = fixture.Create<Half>();
        using var writer = new PooledArrayBufferWriter<byte>(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        writer.Write(expected);
        var read = writer.WrittenSpan.ReadHalf(out var actual);

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        read.Should().Be(writer.WrittenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_Float()
    {
        // Arrange
        var expected = fixture.Create<float>();
        using var writer = new PooledArrayBufferWriter<byte>(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        writer.Write(expected);
        var read = writer.WrittenSpan.ReadFloat(out var actual);

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        read.Should().Be(writer.WrittenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_Double()
    {
        // Arrange
        var expected = fixture.Create<double>();
        using var writer = new PooledArrayBufferWriter<byte>(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        writer.Write(expected);
        var read = writer.WrittenSpan.ReadDouble(out var actual);

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        read.Should().Be(writer.WrittenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_Decimal()
    {
        // Arrange
        var expected = fixture.Create<decimal>();
        using var writer = new PooledArrayBufferWriter<byte>(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        writer.Write(expected);
        var read = writer.WrittenSpan.ReadDecimal(out var actual);

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        read.Should().Be(writer.WrittenCount);
        actual.Should().Be(expected);
    }
}
