using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace Eryri.Buffers.Extensions;

public static class BufferWriterExtensions
{
    extension(IBufferWriter<byte> writer)
    {
        public void Write(byte value)
        {
            writer.GetSpan(1)[0] = value;
            writer.Advance(1);
        }

        public void Write(string value) => writer.Write(value, Encoding.UTF8);
        public void Write(string value, Encoding encoding)
        {
            var byteCount = encoding.GetByteCount(value);
            writer.Write(byteCount);
            var span = writer.GetSpan(byteCount);
            encoding.GetBytes(value, span);
            writer.Advance(byteCount);
        }

        public void Write(char value)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(writer.GetSpan(2), value);
            writer.Advance(2);
        }

        public void Write(Guid value)
        {
            value.TryWriteBytes(writer.GetSpan(16));
            writer.Advance(16);
        }

        public void Write(bool value)
        {
            BitConverter.TryWriteBytes(writer.GetSpan(1), value);
            writer.Advance(1);
        }

        public void Write(short value)
        {
            BinaryPrimitives.WriteInt16LittleEndian(writer.GetSpan(2), value);
            writer.Advance(2);
        }

        public void Write(ushort value)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(writer.GetSpan(2), value);
            writer.Advance(2);
        }

        public void Write(int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(writer.GetSpan(4), value);
            writer.Advance(4);
        }

        public void Write(uint value)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(writer.GetSpan(4), value);
            writer.Advance(4);
        }

        public void Write(long value)
        {
            BinaryPrimitives.WriteInt64LittleEndian(writer.GetSpan(8), value);
            writer.Advance(8);
        }

        public void Write(ulong value)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(writer.GetSpan(8), value);
            writer.Advance(8);
        }

        public void Write(Half value)
        {
            BinaryPrimitives.WriteHalfLittleEndian(writer.GetSpan(2), value);
            writer.Advance(2);
        }

        public void Write(float value)
        {
            BinaryPrimitives.WriteSingleLittleEndian(writer.GetSpan(4), value);
            writer.Advance(4);
        }

        public void Write(double value)
        {
            BinaryPrimitives.WriteDoubleLittleEndian(writer.GetSpan(8), value);
            writer.Advance(8);
        }

        public void Write(decimal value)
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
    }
}
