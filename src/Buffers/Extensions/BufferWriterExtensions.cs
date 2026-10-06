using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace Eryri.Buffers.Extensions;

public static class BufferWriterExtensions
{
    public static void Write(this IBufferWriter<byte> writer, byte value)
    {
        writer.GetSpan(1)[0] = value;
        writer.Advance(1);
    }

    public static void Write(this IBufferWriter<byte> writer, string value) => writer.Write(value, Encoding.UTF8);
    public static void Write(this IBufferWriter<byte> writer, string value, Encoding encoding)
    {
        var byteCount = encoding.GetByteCount(value);
        writer.Write(byteCount);
        var span = writer.GetSpan(byteCount);
        encoding.GetBytes(value, span);
        writer.Advance(byteCount);
    }

    public static void Write(this IBufferWriter<byte> writer, char value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(writer.GetSpan(2), value);
        writer.Advance(2);
    }

    public static void Write(this IBufferWriter<byte> writer, Guid value)
    {
        value.TryWriteBytes(writer.GetSpan(16));
        writer.Advance(16);
    }

    public static void Write(this IBufferWriter<byte> writer, bool value)
    {
        BitConverter.TryWriteBytes(writer.GetSpan(1), value);
        writer.Advance(1);
    }

    public static void Write(this IBufferWriter<byte> writer, short value)
    {
        BinaryPrimitives.WriteInt16LittleEndian(writer.GetSpan(2), value);
        writer.Advance(2);
    }

    public static void Write(this IBufferWriter<byte> writer, ushort value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(writer.GetSpan(2), value);
        writer.Advance(2);
    }

    public static void Write(this IBufferWriter<byte> writer, int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(writer.GetSpan(4), value);
        writer.Advance(4);
    }

    public static void Write(this IBufferWriter<byte> writer, uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(writer.GetSpan(4), value);
        writer.Advance(4);
    }

    public static void Write(this IBufferWriter<byte> writer, long value)
    {
        BinaryPrimitives.WriteInt64LittleEndian(writer.GetSpan(8), value);
        writer.Advance(8);
    }

    public static void Write(this IBufferWriter<byte> writer, ulong value)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(writer.GetSpan(8), value);
        writer.Advance(8);
    }

    public static void Write(this IBufferWriter<byte> writer, Half value)
    {
        BinaryPrimitives.WriteHalfLittleEndian(writer.GetSpan(2), value);
        writer.Advance(2);
    }

    public static void Write(this IBufferWriter<byte> writer, float value)
    {
        BinaryPrimitives.WriteSingleLittleEndian(writer.GetSpan(4), value);
        writer.Advance(4);
    }

    public static void Write(this IBufferWriter<byte> writer, double value)
    {
        BinaryPrimitives.WriteDoubleLittleEndian(writer.GetSpan(8), value);
        writer.Advance(8);
    }

    public static void Write(this IBufferWriter<byte> writer, decimal value)
    {
        var buffer = writer.GetSpan(16);
        Span<int> bits = stackalloc int[4];
        decimal.GetBits(value, bits);

        BinaryPrimitives.WriteInt32LittleEndian(buffer[0..4], bits[0]);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[4..8], bits[1]);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[8..12], bits[2]);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[12..16], bits[3]);

        writer.Advance(16);
    }

    public static int ReadByte(this ReadOnlySpan<byte> reader, out byte value)
    {
        value = reader[0];
        return 1;
    }

    public static int ReadString(this ReadOnlySpan<byte> reader, out string value) => reader.ReadString(out value, Encoding.UTF8);
    public static int ReadString(this ReadOnlySpan<byte> reader, out string value, Encoding encoding)
    {
        var read = reader.ReadInt(out var length);
        value = encoding.GetString(reader.Slice(read, length));
        return read + length;
    }

    public static int ReadChar(this ReadOnlySpan<byte> reader, out char value)
    {
        value = (char)BinaryPrimitives.ReadUInt16LittleEndian(reader[..2]);
        return 2;
    }

    public static int ReadGuid(this ReadOnlySpan<byte> reader, out Guid value)
    {
        value = new Guid(reader[..16]);
        return 16;
    }

    public static int ReadBoolean(this ReadOnlySpan<byte> reader, out bool value)
    {
        value = BitConverter.ToBoolean(reader[..1]);
        return 1;
    }

    public static int ReadShort(this ReadOnlySpan<byte> reader, out short value)
    {
        value = BinaryPrimitives.ReadInt16LittleEndian(reader.Slice(0, 2));
        return 2;
    }

    public static int ReadUShort(this ReadOnlySpan<byte> reader, out ushort value)
    {
        value = BinaryPrimitives.ReadUInt16LittleEndian(reader.Slice(0, 2));
        return 2;
    }

    public static int ReadInt(this ReadOnlySpan<byte> reader, out int value)
    {
        value = BinaryPrimitives.ReadInt32LittleEndian(reader.Slice(0, 4));
        return 4;
    }

    public static int ReadUInt(this ReadOnlySpan<byte> reader, out uint value)
    {
        value = BinaryPrimitives.ReadUInt32LittleEndian(reader.Slice(0, 4));
        return 4;
    }

    public static int ReadLong(this ReadOnlySpan<byte> reader, out long value)
    {
        value = BinaryPrimitives.ReadInt64LittleEndian(reader.Slice(0, 8));
        return 8;
    }

    public static int ReadULong(this ReadOnlySpan<byte> reader, out ulong value)
    {
        value = BinaryPrimitives.ReadUInt64LittleEndian(reader.Slice(0, 8));
        return 8;
    }

    public static int ReadHalf(this ReadOnlySpan<byte> reader, out Half value)
    {
        value = BinaryPrimitives.ReadHalfLittleEndian(reader.Slice(0, 2));
        return 2;
    }

    public static int ReadFloat(this ReadOnlySpan<byte> reader, out float value)
    {
        value = BinaryPrimitives.ReadSingleLittleEndian(reader.Slice(0, 4));
        return 4;
    }

    public static int ReadDouble(this ReadOnlySpan<byte> reader, out double value)
    {
        value = BinaryPrimitives.ReadDoubleLittleEndian(reader.Slice(0, 8));
        return 8;
    }

    public static int ReadDecimal(this ReadOnlySpan<byte> reader, out decimal value)
    {
        Span<int> bits = stackalloc int[4];
        bits[0] = BinaryPrimitives.ReadInt32LittleEndian(reader[0..4]);
        bits[1] = BinaryPrimitives.ReadInt32LittleEndian(reader[4..8]);
        bits[2] = BinaryPrimitives.ReadInt32LittleEndian(reader[8..12]);
        bits[3] = BinaryPrimitives.ReadInt32LittleEndian(reader[12..16]);

        value = new decimal(bits);
        return 16;
    }
}
