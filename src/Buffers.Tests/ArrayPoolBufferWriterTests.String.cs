using System.Buffers;
using System.Diagnostics;
using System.Text;

namespace Eryri.Buffers.Tests;

public class ArrayPoolBufferWriterTests_String : ArrayPoolBufferWriterTests<string>
{
    protected override void WriteData(IBufferWriter<string> bufferWriter, int numStrings)
    {
        Span<string> outputSpan = bufferWriter.GetSpan(numStrings);
        Debug.Assert(outputSpan.Length >= numStrings);
        var random = new Random(42);

        var data = new string[numStrings];

        for (int i = 0; i < numStrings; i++)
        {
            int length = random.Next(5, 10);
            data[i] = GetRandomString(random, length, 32, 127);
        }

        data.CopyTo(outputSpan);

        bufferWriter.Advance(numStrings);
    }

    private static string GetRandomString(Random r, int length, int minCodePoint, int maxCodePoint)
    {
        StringBuilder sb = new StringBuilder(length);
        while (length-- != 0)
        {
            sb.Append((char)r.Next(minCodePoint, maxCodePoint));
        }

        return sb.ToString();
    }
}