using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Eryri.Buffers.Extensions;

public static partial class StreamExtensions
{
    extension(Stream stream)
    {
        /// <summary>
        /// Gets the written array or data.
        /// </summary>
        public byte[] ToArray()
        {
            if (stream.Length == 0)
            {
                return Array.Empty<byte>();
            }

            var destination = new byte[stream.Length];
            var position = stream.Position;
            stream.Seek(0, SeekOrigin.Begin);

            stream.ReadExactly(destination.AsSpan());

            stream.Seek(position, SeekOrigin.Begin);
            return destination;
        }
    }
}
