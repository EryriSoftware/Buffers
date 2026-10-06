using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace Eryri.Buffers.Extensions;

public static class StreamExtensions
{
    public static void Write(this Stream stream, byte value) => stream.WriteByte(value);

    public static void Write(this Stream stream, string value) => stream.Write(value, Encoding.UTF8);
    public static void Write(this Stream stream, string value, Encoding encoding)
    {
        var length = encoding.GetByteCount(value);
        stream.Write(length);

        using var buffer = MemoryPool<byte>.Shared.Rent(length);
        var span = buffer.Memory.Span.Slice(0, length);
        encoding.GetBytes(value, span);
        stream.Write(span);
    }

    public static void Write(this Stream stream, char value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    public static void Write(this Stream stream, Guid value)
    {
        Span<byte> buffer = stackalloc byte[16];
        value.TryWriteBytes(buffer);
        stream.Write(buffer);
    }

    public static void Write(this Stream stream, bool value)
    {
        Span<byte> buffer = stackalloc byte[1];
        BitConverter.TryWriteBytes(buffer, value);
        stream.Write(buffer);
    }

    public static void Write(this Stream stream, short value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteInt16LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    public static void Write(this Stream stream, ushort value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    public static void Write(this Stream stream, int value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    public static void Write(this Stream stream, uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    public static void Write(this Stream stream, long value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    public static void Write(this Stream stream, ulong value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    public static void Write(this Stream stream, Half value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteHalfLittleEndian(buffer, value);
        stream.Write(buffer);
    }

    public static void Write(this Stream stream, float value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteSingleLittleEndian(buffer, value);
        stream.Write(buffer);
    }

    public static void Write(this Stream stream, double value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteDoubleLittleEndian(buffer, value);
        stream.Write(buffer);
    }

    public static void Write(this Stream stream, decimal value)
    {
        Span<byte> buffer = stackalloc byte[16];
        Span<int> bits = stackalloc int[4];
        decimal.GetBits(value, bits);

        BinaryPrimitives.WriteInt32LittleEndian(buffer[0..4], bits[0]);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[4..8], bits[1]);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[8..12], bits[2]);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[12..16], bits[3]);

        stream.Write(buffer);
    }

    public static string ReadString(this Stream stream) => stream.ReadString(Encoding.UTF8);
    public static string ReadString(this Stream stream, Encoding encoding)
    {
        var length = stream.ReadInt();
        using var buffer = MemoryPool<byte>.Shared.Rent(length);
        var span = buffer.Memory.Span.Slice(0, length);
        stream.ReadExactly(span);
        return encoding.GetString(span);
    }

    public static char ReadChar(this Stream stream)
    {
        Span<byte> buffer = stackalloc byte[2];
        stream.ReadExactly(buffer);
        return (char)BinaryPrimitives.ReadUInt16LittleEndian(buffer);
    }

    public static Guid ReadGuid(this Stream stream)
    {
        Span<byte> buffer = stackalloc byte[16];
        stream.ReadExactly(buffer);
        return new Guid(buffer);
    }

    public static bool ReadBoolean(this Stream stream)
    {
        Span<byte> buffer = stackalloc byte[1];
        stream.ReadExactly(buffer);
        return BitConverter.ToBoolean(buffer);
    }

    public static short ReadShort(this Stream stream)
    {
        Span<byte> buffer = stackalloc byte[2];
        stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadInt16LittleEndian(buffer);
    }

    public static ushort ReadUShort(this Stream stream)
    {
        Span<byte> buffer = stackalloc byte[2];
        stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadUInt16LittleEndian(buffer);
    }

    public static int ReadInt(this Stream stream)
    {
        Span<byte> buffer = stackalloc byte[4];
        stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadInt32LittleEndian(buffer);
    }

    public static uint ReadUInt(this Stream stream)
    {
        Span<byte> buffer = stackalloc byte[4];
        stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadUInt32LittleEndian(buffer);
    }

    public static long ReadLong(this Stream stream)
    {
        Span<byte> buffer = stackalloc byte[8];
        stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadInt64LittleEndian(buffer);
    }

    public static ulong ReadULong(this Stream stream)
    {
        Span<byte> buffer = stackalloc byte[8];
        stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadUInt64LittleEndian(buffer);
    }

    public static Half ReadHalf(this Stream stream)
    {
        Span<byte> buffer = stackalloc byte[2];
        stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadHalfLittleEndian(buffer);
    }

    public static float ReadFloat(this Stream stream)
    {
        Span<byte> buffer = stackalloc byte[4];
        stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadSingleLittleEndian(buffer);
    }

    public static double ReadDouble(this Stream stream)
    {
        Span<byte> buffer = stackalloc byte[8];
        stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadDoubleLittleEndian(buffer);
    }

    public static decimal ReadDecimal(this Stream stream)
    {
        Span<byte> buffer = stackalloc byte[16];
        stream.ReadExactly(buffer);

        Span<int> bits = stackalloc int[4];
        bits[0] = BinaryPrimitives.ReadInt32LittleEndian(buffer[0..4]);
        bits[1] = BinaryPrimitives.ReadInt32LittleEndian(buffer[4..8]);
        bits[2] = BinaryPrimitives.ReadInt32LittleEndian(buffer[8..12]);
        bits[3] = BinaryPrimitives.ReadInt32LittleEndian(buffer[12..16]);

        return new decimal(bits);
    }
}
