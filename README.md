# [Eryri.Buffers](https://www.nuget.org/packages/Eryri.Buffers)

[![NuGet](https://img.shields.io/nuget/v/Eryri.Buffers.svg)](https://www.nuget.org/packages/Eryri.Buffers)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Eryri.Buffers.svg)](https://www.nuget.org/packages/Eryri.Buffers)

**Build and parse binary data with fewer allocations and full control over the format.**

`Eryri.Buffers` provides pooled buffers and low-allocation binary read/write extensions for .NET. Write primitive values directly to a buffer or stream, parse data from a span, or use a pooled, seekable stream when you need a growable in-memory destination.

* **Build payloads efficiently** with `ArrayPoolBufferWriter<T>`.
* **Buffer stream output** with `MemoryPoolStream`, backed by `ArrayPool<byte>`.
* **Read and write binary primitives** without a serializer framework or intermediate byte arrays.
* **Define your own wire format**, including field order, string encoding and versioning.

Useful for network protocols, message processing, file formats, caches and other workloads where temporary allocations and binary serialization overhead matter.

---

## Install

```bash
dotnet add package Eryri.Buffers
```

```csharp
using Eryri.Buffers;
using Eryri.Buffers.Extensions;
```

## Choose your buffer

| Need                                                                    | Use                             |
| ------------------------------------------------------------------------| ------------------------------- |
| Build a binary payload with direct access to writable memory            | `ArrayPoolBufferWriter<byte>`   |
| Use a seekable, growable in-memory stream                               | `MemoryPoolStream`              |
| Write binary values to an existing stream                               | Stream extensions               |
| Parse binary data without a reader object                               | `ReadOnlySpan<byte>` extensions |
| Write to any `IBufferWriter<byte>`                                      | Buffer-writer extensions        |

## Build a binary payload

Use `ArrayPoolBufferWriter<byte>` when you want to construct a contiguous payload without allocating a new backing array for every message.

```csharp
using var writer = new ArrayPoolBufferWriter<byte>(256);

writer.Write(42);
writer.Write(DateTime.UtcNow.Ticks);
writer.Write(123.45);
writer.Write("Hello, world!");

ReadOnlySpan<byte> payload = writer.WrittenSpan;
```

The written data can be passed directly to APIs accepting `ReadOnlySpan<byte>` or `ReadOnlyMemory<byte>`. The writer grows automatically when necessary and returns its rented storage when disposed.

Supply an initial capacity when you have a reasonable estimate of the payload size to reduce growth and copying.

## Buffer output in a stream

Use `MemoryPoolStream` when an API expects a `Stream`, but you want its contents held in pooled, growable memory rather than a `MemoryStream`-allocated backing array.

```csharp
using var stream = new MemoryPoolStream(1024);

stream.Write(42);
stream.Write("Hello, world!");

stream.Position = 0;

var number = stream.ReadInt();
var message = stream.ReadString();

byte[] payload = stream.ToArray();
```

`MemoryPoolStream` supports reading, writing, seeking, changing length and automatic capacity growth. Seeking beyond the end does not extend the stream; writing beyond the end fills the gap with zeroes.

The stream rents its backing array from `ArrayPool<byte>.Shared` and returns it when disposed. `ToArray()` creates an independent copy of the written contents; use it when you need an owned array beyond the stream's lifetime.

## Write and read binary values

The binary extensions work with streams, `IBufferWriter<byte>` implementations and `ReadOnlySpan<byte>`.

### Write to a buffer or stream

```csharp
using var writer = new ArrayPoolBufferWriter<byte>();

writer.Write(42);
writer.Write(123.456);
writer.Write(Guid.NewGuid());
writer.Write(true);
writer.Write("Hello, world!");
```

The same write operations can target an existing stream:

```csharp
stream.Write(42);
stream.Write(123.456);
stream.Write(Guid.NewGuid());
stream.Write("Hello, world!");
```

Buffer-writer extensions accept any `IBufferWriter<byte>`, so application code doesn't have to depend on a particular buffer implementation.

### Parse a span

```csharp
ReadOnlySpan<byte> data = /* binary payload */;

int offset = 0;
offset += data.ReadInt(out var id);
offset += data.ReadLong(out var timestamp);
offset += data.ReadDouble(out var amount);
offset += data.ReadString(out var name);
```

Each reader returns the number of bytes consumed, allowing sequential parsing without a separate reader object.

## Binary format

The extensions use explicit, predictable representations:

* **Numeric values:** little-endian encoding.
* **Strings:** length-prefixed by encoded byte count; UTF-8 by default, with support for explicit encodings.
* **`Guid`:** the representation produced by .NET's `Guid.TryWriteBytes`.
* **`decimal`:** four 32-bit values from `decimal.GetBits`.
* **`char`:** a two-byte UTF-16 code unit.

Supported types include `byte`, `bool`, `char`, `short`, `ushort`, `Half`, `int`, `uint`, `float`, `long`, `ulong`, `double`, `decimal`, `Guid` and `string`.

Use the matching reader and writer, and the same string encoding, to preserve compatibility.

## Designed for low-allocation binary processing

* Fixed-size values are encoded directly into destination spans.
* Span-based reads avoid intermediate buffers.
* Pooled buffers reuse backing storage and grow when needed.
* Stream string encoding uses pooled temporary memory rather than a temporary managed byte array for encoded bytes.

Pooling reduces allocation pressure, but growth can still require copying, and APIs such as `ToArray()` deliberately allocate an owned copy.

## What it isn't

`Eryri.Buffers` is a set of low-level binary building blocks, not an automatic object serializer. It doesn't discover object members, generate schemas or manage serialization versioning for you.

You control the format; the library helps you build and parse it efficiently.
