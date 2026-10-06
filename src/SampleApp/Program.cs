using System.Diagnostics;
using Eryri.Buffers;
using Eryri.Buffers.Extensions;

using var writer = new ArrayPoolBufferWriter<byte>();
var id = Guid.NewGuid();

writer.Write(42);
writer.Write("Hello, World!");
writer.Write(id);

var span = writer.WrittenSpan;
var offset = 0;
offset += span.Slice(offset).ReadInt(out var integer);
offset += span.Slice(offset).ReadString(out var message);
offset += span.Slice(offset).ReadGuid(out var guid);

Debug.Assert(integer == 42, "Unable to write and read an integer");
Debug.Assert(message == "Hello, World!", "Unable to write and read a string");
Debug.Assert(guid == id, "Unable to write and read a guid");

using var stream = new MemoryStream();
stream.Write(writer.WrittenSpan);
stream.Seek(0, SeekOrigin.Begin);

integer = stream.ReadInt();
message = stream.ReadString();
guid = stream.ReadGuid();

Debug.Assert(integer == 42, "Unable to write and read an integer");
Debug.Assert(message == "Hello, World!", "Unable to write and read a string");
Debug.Assert(guid == id, "Unable to write and read a guid");

Console.WriteLine("Success!");
