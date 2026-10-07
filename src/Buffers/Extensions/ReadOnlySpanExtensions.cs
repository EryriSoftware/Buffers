using System.Buffers.Binary;
using System.Text;

namespace Eryri.Buffers.Extensions;

public static class ReadOnlySpanExtensions
{
    extension(ReadOnlySpan<byte> reader)
    {
        public int ReadByte(out byte value)
        {
            value = reader[0];
            return 1;
        }

        public int ReadString(out string value) => reader.ReadString(out value, Encoding.UTF8);
        public int ReadString(out string value, Encoding encoding)
        {
            var read = reader.ReadInt(out var length);
            value = encoding.GetString(reader.Slice(read, length));
            return read + length;
        }

        public int ReadChar(out char value)
        {
            value = (char)BinaryPrimitives.ReadUInt16LittleEndian(reader[..2]);
            return 2;
        }

        public int ReadGuid(out Guid value)
        {
            value = new Guid(reader[..16]);
            return 16;
        }

        public int ReadBoolean(out bool value)
        {
            value = BitConverter.ToBoolean(reader[..1]);
            return 1;
        }

        public int ReadShort(out short value)
        {
            value = BinaryPrimitives.ReadInt16LittleEndian(reader.Slice(0, 2));
            return 2;
        }

        public int ReadUShort(out ushort value)
        {
            value = BinaryPrimitives.ReadUInt16LittleEndian(reader.Slice(0, 2));
            return 2;
        }

        public int ReadInt(out int value)
        {
            value = BinaryPrimitives.ReadInt32LittleEndian(reader.Slice(0, 4));
            return 4;
        }

        public int ReadUInt(out uint value)
        {
            value = BinaryPrimitives.ReadUInt32LittleEndian(reader.Slice(0, 4));
            return 4;
        }

        public int ReadLong(out long value)
        {
            value = BinaryPrimitives.ReadInt64LittleEndian(reader.Slice(0, 8));
            return 8;
        }

        public int ReadULong(out ulong value)
        {
            value = BinaryPrimitives.ReadUInt64LittleEndian(reader.Slice(0, 8));
            return 8;
        }

        public int ReadHalf(out Half value)
        {
            value = BinaryPrimitives.ReadHalfLittleEndian(reader.Slice(0, 2));
            return 2;
        }

        public int ReadFloat(out float value)
        {
            value = BinaryPrimitives.ReadSingleLittleEndian(reader.Slice(0, 4));
            return 4;
        }

        public int ReadDouble(out double value)
        {
            value = BinaryPrimitives.ReadDoubleLittleEndian(reader.Slice(0, 8));
            return 8;
        }

        public int ReadDecimal(out decimal value)
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
}
