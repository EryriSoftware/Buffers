using AutoFixture;
using Eryri.Buffers.Extensions;
using FluentAssertions;

namespace Eryri.Buffers.Tests.Extensions;

[TestFixture, Parallelizable(ParallelScope.All)]
internal class StreamExtensionsTests
{
    private Fixture fixture = new Fixture();
    private const int BufferSize = 1 << 10; // 1 KiB

    [Test]
    public void Can_Write_And_Read_Byte()
    {
        // Arrange
        var expected = fixture.Create<byte>();
        using var stream = new MemoryStream(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        stream.Write(expected);
        var writtenCount = stream.Position;
        stream.Seek(0, SeekOrigin.Begin);
        var actual = stream.ReadByte();

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        stream.Position.Should().Be(writtenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_String()
    {
        // Arrange
        var expected = fixture.Create<string>();
        using var stream = new MemoryStream(BufferSize);

        // Act
        stream.Write(expected);
        var writtenCount = stream.Position;
        stream.Seek(0, SeekOrigin.Begin);
        var actual = stream.ReadString();

        // Assert
        stream.Position.Should().Be(writtenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_Char()
    {
        // Arrange
        var expected = "😀"[0];
        using var stream = new MemoryStream(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        stream.Write(expected);
        var writtenCount = stream.Position;
        stream.Seek(0, SeekOrigin.Begin);
        var actual = stream.ReadChar();

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        stream.Position.Should().Be(writtenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_Guid()
    {
        // Arrange
        var expected = fixture.Create<Guid>();
        using var stream = new MemoryStream(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        stream.Write(expected);
        var writtenCount = stream.Position;
        stream.Seek(0, SeekOrigin.Begin);
        var actual = stream.ReadGuid();

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        stream.Position.Should().Be(writtenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_Bool()
    {
        // Arrange
        var expected = fixture.Create<bool>();
        using var stream = new MemoryStream(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        stream.Write(expected);
        var writtenCount = stream.Position;
        stream.Seek(0, SeekOrigin.Begin);
        var actual = stream.ReadBoolean();

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        stream.Position.Should().Be(writtenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_Short()
    {
        // Arrange
        var expected = fixture.Create<short>();
        using var stream = new MemoryStream(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        stream.Write(expected);
        var writtenCount = stream.Position;
        stream.Seek(0, SeekOrigin.Begin);
        var actual = stream.ReadShort();

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        stream.Position.Should().Be(writtenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_UShort()
    {
        // Arrange
        var expected = fixture.Create<ushort>();
        using var stream = new MemoryStream(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        stream.Write(expected);
        var writtenCount = stream.Position;
        stream.Seek(0, SeekOrigin.Begin);
        var actual = stream.ReadUShort();

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        stream.Position.Should().Be(writtenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_Int()
    {
        // Arrange
        var expected = fixture.Create<int>();
        using var stream = new MemoryStream(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        stream.Write(expected);
        var writtenCount = stream.Position;
        stream.Seek(0, SeekOrigin.Begin);
        var actual = stream.ReadInt();

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        stream.Position.Should().Be(writtenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_UInt()
    {
        // Arrange
        var expected = fixture.Create<uint>();
        using var stream = new MemoryStream(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        stream.Write(expected);
        var writtenCount = stream.Position;
        stream.Seek(0, SeekOrigin.Begin);
        var actual = stream.ReadUInt();

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        stream.Position.Should().Be(writtenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_Long()
    {
        // Arrange
        var expected = fixture.Create<long>();
        using var stream = new MemoryStream(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        stream.Write(expected);
        var writtenCount = stream.Position;
        stream.Seek(0, SeekOrigin.Begin);
        var actual = stream.ReadLong();

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        stream.Position.Should().Be(writtenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_ULong()
    {
        // Arrange
        var expected = fixture.Create<ulong>();
        using var stream = new MemoryStream(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        stream.Write(expected);
        var writtenCount = stream.Position;
        stream.Seek(0, SeekOrigin.Begin);
        var actual = stream.ReadULong();

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        stream.Position.Should().Be(writtenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_Half()
    {
        // Arrange
        var expected = fixture.Create<Half>();
        using var stream = new MemoryStream(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        stream.Write(expected);
        var writtenCount = stream.Position;
        stream.Seek(0, SeekOrigin.Begin);
        var actual = stream.ReadHalf();

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        stream.Position.Should().Be(writtenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_Float()
    {
        // Arrange
        var expected = fixture.Create<float>();
        using var stream = new MemoryStream(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        stream.Write(expected);
        var writtenCount = stream.Position;
        stream.Seek(0, SeekOrigin.Begin);
        var actual = stream.ReadFloat();

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        stream.Position.Should().Be(writtenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_Double()
    {
        // Arrange
        var expected = fixture.Create<double>();
        using var stream = new MemoryStream(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        stream.Write(expected);
        var writtenCount = stream.Position;
        stream.Seek(0, SeekOrigin.Begin);
        var actual = stream.ReadDouble();

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        stream.Position.Should().Be(writtenCount);
        actual.Should().Be(expected);
    }

    [Test]
    public void Can_Write_And_Read_Decimal()
    {
        // Arrange
        var expected = fixture.Create<decimal>();
        using var stream = new MemoryStream(BufferSize);
        var before = GC.GetAllocatedBytesForCurrentThread();

        // Act
        stream.Write(expected);
        var writtenCount = stream.Position;
        stream.Seek(0, SeekOrigin.Begin);
        var actual = stream.ReadDecimal();

        // Assert
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        stream.Position.Should().Be(writtenCount);
        actual.Should().Be(expected);
    }
}
