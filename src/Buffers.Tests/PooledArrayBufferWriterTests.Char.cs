using System.Buffers;
using System.Diagnostics;

namespace Eryri.Buffers.Tests
{
    public class PooledArrayBufferWriterTests_Char : PooledArrayBufferWriterTests<char>
    {
        protected override void WriteData(IBufferWriter<char> bufferWriter, int numChars)
        {
            Span<char> outputSpan = bufferWriter.GetSpan(numChars);
            Debug.Assert(outputSpan.Length >= numChars);
            var random = new Random(42);

            var data = new char[numChars];

            for (int i = 0; i < numChars; i++)
            {
                data[i] = (char)random.Next(0, char.MaxValue);
            }

            data.CopyTo(outputSpan);

            bufferWriter.Advance(numChars);
        }
    }
}