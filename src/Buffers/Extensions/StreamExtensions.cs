using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace Eryri.Buffers.Extensions;

public static class StreamExtensions
{
    extension(Stream stream)
    {
        public void Write(byte value) => stream.WriteByte(value);

        public void Write(string value) => stream.Write(value, Encoding.UTF8);
        public void Write(string value, Encoding encoding)
        {
            var length = encoding.GetByteCount(value);
            stream.Write(length);

            using var buffer = MemoryPool<byte>.Shared.Rent(length);
            var span = buffer.Memory.Span.Slice(0, length);
            encoding.GetBytes(value, span);
            stream.Write(span);
        }

        public void Write(char value)
        {
            Span<byte> buffer = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
            stream.Write(buffer);
        }

        public void Write(Guid value)
        {
            Span<byte> buffer = stackalloc byte[16];
            value.TryWriteBytes(buffer);
            stream.Write(buffer);
        }

        public void Write(bool value)
        {
            Span<byte> buffer = stackalloc byte[1];
            BitConverter.TryWriteBytes(buffer, value);
            stream.Write(buffer);
        }

        public void Write(short value)
        {
            Span<byte> buffer = stackalloc byte[2];
            BinaryPrimitives.WriteInt16LittleEndian(buffer, value);
            stream.Write(buffer);
        }

        public void Write(ushort value)
        {
            Span<byte> buffer = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
            stream.Write(buffer);
        }

        public void Write(int value)
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            stream.Write(buffer);
        }

        public void Write(uint value)
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
            stream.Write(buffer);
        }

        public void Write(long value)
        {
            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
            stream.Write(buffer);
        }

        public void Write(ulong value)
        {
            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
            stream.Write(buffer);
        }

        public void Write(Half value)
        {
            Span<byte> buffer = stackalloc byte[2];
            BinaryPrimitives.WriteHalfLittleEndian(buffer, value);
            stream.Write(buffer);
        }

        public void Write(float value)
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteSingleLittleEndian(buffer, value);
            stream.Write(buffer);
        }

        public void Write(double value)
        {
            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteDoubleLittleEndian(buffer, value);
            stream.Write(buffer);
        }

        public void Write(decimal value)
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

        public string ReadString() => stream.ReadString(Encoding.UTF8);
        public string ReadString(Encoding encoding)
        {
            var length = stream.ReadInt();
            using var buffer = MemoryPool<byte>.Shared.Rent(length);
            var span = buffer.Memory.Span.Slice(0, length);
            stream.ReadExactly(span);
            return encoding.GetString(span);
        }

        public char ReadChar()
        {
            Span<byte> buffer = stackalloc byte[2];
            stream.ReadExactly(buffer);
            return (char)BinaryPrimitives.ReadUInt16LittleEndian(buffer);
        }

        public Guid ReadGuid()
        {
            Span<byte> buffer = stackalloc byte[16];
            stream.ReadExactly(buffer);
            return new Guid(buffer);
        }

        public bool ReadBoolean()
        {
            Span<byte> buffer = stackalloc byte[1];
            stream.ReadExactly(buffer);
            return BitConverter.ToBoolean(buffer);
        }

        public short ReadShort()
        {
            Span<byte> buffer = stackalloc byte[2];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadInt16LittleEndian(buffer);
        }

        public ushort ReadUShort()
        {
            Span<byte> buffer = stackalloc byte[2];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadUInt16LittleEndian(buffer);
        }

        public int ReadInt()
        {
            Span<byte> buffer = stackalloc byte[4];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }

        public uint ReadUInt()
        {
            Span<byte> buffer = stackalloc byte[4];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        }

        public long ReadLong()
        {
            Span<byte> buffer = stackalloc byte[8];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadInt64LittleEndian(buffer);
        }

        public ulong ReadULong()
        {
            Span<byte> buffer = stackalloc byte[8];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        }

        public Half ReadHalf()
        {
            Span<byte> buffer = stackalloc byte[2];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadHalfLittleEndian(buffer);
        }

        public float ReadFloat()
        {
            Span<byte> buffer = stackalloc byte[4];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadSingleLittleEndian(buffer);
        }

        public double ReadDouble()
        {
            Span<byte> buffer = stackalloc byte[8];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadDoubleLittleEndian(buffer);
        }

        public decimal ReadDecimal()
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
}
