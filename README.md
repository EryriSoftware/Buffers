# [Eryri.Buffers](https://www.nuget.org/packages/Eryri.Buffers)

[![NuGet](https://img.shields.io/nuget/v/Eryri.Buffers.svg)](https://www.nuget.org/packages/Eryri.Buffers)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Eryri.Buffers.svg)](https://www.nuget.org/packages/Eryri.Buffers)

**Build binary data without constantly allocating temporary buffers.** Eryri.Buffers provides pooled, growable buffers and low-allocation binary read/write extensions for .NET.

Write primitive values directly to a `Stream`, `IBufferWriter<byte>`, or `ReadOnlySpan<byte>` without introducing a serializer framework or intermediate `byte[]` buffers.

Use `PooledArrayBufferWriter<T>` when you need a reusable contiguous buffer backed by `ArrayPool<T>`, or use the binary extensions when you simply need a fast, explicit way to encode and decode primitive values.

It is designed for **high-throughput infrastructure** where buffer allocation, copying and serialization overhead matter: network protocols, message payloads, caches, write-ahead logs, file formats and other binary data pipelines.

## Why use it?

* **Build buffers without allocating a new array every time.** `PooledArrayBufferWriter<T>` rents its backing storage from `ArrayPool<T>` and returns it when disposed.
* **Write directly into the destination.** The `IBufferWriter<byte>` extensions obtain writable spans from the destination and encode values directly into them.
* **Avoid intermediate serialization buffers.** Write a message directly into a pooled buffer and consume its `WrittenSpan` or `WrittenMemory`.
* **Use the same binary primitives with streams.** Encode and decode common .NET types directly against a `Stream`.
* **Parse binary data without a reader object.** `ReadOnlySpan<byte>` extensions return the number of bytes consumed, making sequential parsing straightforward.
* **Keep the wire format explicit.** Numeric values use little-endian encoding and strings use an explicit byte-length prefix.
* **Stay below the serializer layer.** There is no reflection, object graph traversal, schema system or generated serialization model. Your application controls the format.
* **Compose with standard .NET APIs.** The library uses `Stream`, `IBufferWriter<T>`, `Span<T>`, `ReadOnlySpan<T>`, `Memory<T>`, `ReadOnlyMemory<T>` and `ArrayPool<T>`.

**Good fit:** applications that construct or parse binary data frequently and want direct control over buffers and the binary format.

**Not a fit:** applications looking for automatic object serialization, schema evolution, polymorphic serialization or a general-purpose serialization framework.

## Scope at a glance

| Requirement                        | Eryri.Buffers                |
| ---------------------------------- | ---------------------------- |
| Pooled contiguous buffers          | Yes                          |
| `IBufferWriter<T>` implementation  | `PooledArrayBufferWriter<T>` |
| `ArrayPool<T>` backing storage     | Yes                          |
| Automatic buffer growth            | Yes                          |
| `Stream` binary read/write         | Yes                          |
| `IBufferWriter<byte>` binary write | Yes                          |
| `ReadOnlySpan<byte>` binary read   | Yes                          |
| UTF-8 strings by default           | Yes                          |
| Custom string encodings            | Yes                          |
| Little-endian numeric encoding     | Yes                          |
| Reflection-based serialization     | No                           |
| Automatic object serialization     | No                           |
| Schema / version management        | No                           |
| Segmented output                   | No; buffers are contiguous   |

## Install and get started

The NuGet package ID is `Eryri.Buffers`.

```bash
dotnet add package Eryri.Buffers
```

The buffer writer namespace is:

```csharp
using Eryri.Buffers;
```

The binary extension methods are:

```csharp
using Eryri.Buffers.Extensions;
```

### Build a binary payload

When you need to construct a binary message, write directly into a pooled buffer:

```csharp
using Eryri.Buffers;
using Eryri.Buffers.Extensions;

using var writer = new PooledArrayBufferWriter<byte>();

writer.Write(42);
writer.Write(DateTime.UtcNow.Ticks);
writer.Write(123.45);
writer.Write("Hello, world!");

ReadOnlySpan<byte> payload = writer.WrittenSpan;
```

The resulting payload is contiguous and can be passed directly to APIs accepting `ReadOnlySpan<byte>` or `ReadOnlyMemory<byte>`.

### Provide an initial capacity

If the approximate size of the payload is known, provide it up front:

```csharp
using var writer = new PooledArrayBufferWriter<byte>(4096);
```

This can avoid buffer growth and the associated copy.

## PooledArrayBufferWriter<T>

`PooledArrayBufferWriter<T>` is a contiguous, growable `IBufferWriter<T>` backed by arrays rented from `ArrayPool<T>`.

It is intended for workloads where buffers are created frequently and allocating a new managed array for every buffer would create unnecessary GC pressure.

```csharp
using Eryri.Buffers;

using var writer = new PooledArrayBufferWriter<byte>();

var span = writer.GetSpan(4);

span[0] = 1;
span[1] = 2;
span[2] = 3;
span[3] = 4;

writer.Advance(4);

ReadOnlySpan<byte> data = writer.WrittenSpan;
```

### Growth

The writer maintains a single contiguous backing array.

When more capacity is required:

1. A larger array is rented from `ArrayPool<T>`.
2. Only the data already written is copied into the new array.
3. The previous array is returned to the pool.
4. Writing continues in the new array.

For example:

```text
Before growth

┌─────────────────────────────────┐
│ written data │ unused capacity │
└─────────────────────────────────┘
       │
       │ rent larger array
       │ copy written data
       ▼
┌─────────────────────────────────────────────┐
│ written data │          unused              │
└─────────────────────────────────────────────┘
       │
       └── return previous array to pool
```

Growth therefore still involves a memory copy. Pooling removes the managed allocation of the new backing array; it does not make a contiguous buffer grow without copying.

If the required capacity is known, supplying an initial capacity avoids the growth copy.

### Disposal

The writer rents its backing storage and must return it to the pool when finished:

```csharp
using var writer = new PooledArrayBufferWriter<byte>();
```

Do not retain references to its written memory after the writer has been disposed.

## Binary extensions

Eryri.Buffers includes explicit binary read/write extensions for common .NET primitive types.

The same binary representations can be used with:

* `Stream`
* `IBufferWriter<byte>`
* `ReadOnlySpan<byte>`

### Write to an IBufferWriter<byte>

```csharp
using Eryri.Buffers;
using Eryri.Buffers.Extensions;

using var writer = new PooledArrayBufferWriter<byte>();

writer.Write(42);
writer.Write(123.456);
writer.Write(Guid.NewGuid());
writer.Write(true);
writer.Write("Hello, world!");

ReadOnlySpan<byte> payload = writer.WrittenSpan;
```

The methods work with any `IBufferWriter<byte>` implementation, not only `PooledArrayBufferWriter<byte>`:

```csharp
void WriteMessage(IBufferWriter<byte> writer)
{
    writer.Write(42);
    writer.Write("Hello");
}
```

This keeps the serialization code independent of the underlying buffer implementation.

### Write to a Stream

The same primitives can be written directly to a stream:

```csharp
stream.Write(42);
stream.Write(123.456);
stream.Write(Guid.NewGuid());
stream.Write("Hello, world!");
```

And read back:

```csharp
var number = stream.ReadInt();
var value = stream.ReadDouble();
var id = stream.ReadGuid();
var message = stream.ReadString();
```

### Read directly from a span

For in-memory binary payloads, values can be decoded directly from a `ReadOnlySpan<byte>`:

```csharp
ReadOnlySpan<byte> payload = ...;

var offset = 0;

offset += payload.ReadInt(out var id);
offset += payload.ReadLong(out var timestamp);
offset += payload.ReadDouble(out var amount);
offset += payload.ReadString(out var name);
```

The methods return the number of bytes consumed.

This makes the API suitable for parsing a sequence of values without creating a reader object or maintaining a separate cursor type.

## Supported binary types

The current primitive extensions support:

| Type      | Binary size |
| --------- | ----------: |
| `byte`    |      1 byte |
| `bool`    |      1 byte |
| `char`    |     2 bytes |
| `short`   |     2 bytes |
| `ushort`  |     2 bytes |
| `Half`    |     2 bytes |
| `int`     |     4 bytes |
| `uint`    |     4 bytes |
| `float`   |     4 bytes |
| `long`    |     8 bytes |
| `ulong`   |     8 bytes |
| `double`  |     8 bytes |
| `decimal` |    16 bytes |
| `Guid`    |    16 bytes |
| `string`  |    Variable |

The binary extensions are intentionally primitive-focused. Higher-level types can be built from these operations by the application.

## Binary format

The binary format is deliberately explicit.

### Numeric values

Integer and floating-point values use **little-endian** encoding.

```text
short    2 bytes
ushort   2 bytes
int      4 bytes
uint     4 bytes
long     8 bytes
ulong    8 bytes
Half     2 bytes
float    4 bytes
double   8 bytes
```

### Decimal

`decimal` is represented using the four 32-bit values returned by `decimal.GetBits`.

Total size: **16 bytes**.

### Guid

`Guid` uses the byte representation produced by .NET's `Guid.TryWriteBytes`.

Total size: **16 bytes**.

The same representation is consumed by the corresponding `Guid` reader.

### Char

A `char` is serialized as its UTF-16 code unit.

Total size: **2 bytes**, little-endian.

A `char` is therefore not encoded as UTF-8. A .NET `char` represents a UTF-16 code unit and can be one half of a surrogate pair.

### Strings

Strings are length-prefixed using the number of **encoded bytes**:

```text
┌──────────────────┬──────────────────────────────┐
│ int32 byte length│ encoded string bytes         │
└──────────────────┴──────────────────────────────┘
```

The default encoding is UTF-8:

```csharp
writer.Write("Hello");
```

The resulting representation is:

```text
05 00 00 00 48 65 6C 6C 6F
```

The encoding can be supplied explicitly:

```csharp
writer.Write(value, Encoding.Unicode);
```

The same encoding must be supplied when reading the value.

## Allocation characteristics

Eryri.Buffers is designed to reduce unnecessary allocations, but not every operation can or should be allocation-free.

### Pooled buffer operations

`PooledArrayBufferWriter<T>` rents its backing arrays from `ArrayPool<T>`.

After the initial rent, writing within the available capacity does not require a new backing-array allocation.

When the buffer grows, the existing written data must be copied into the new contiguous buffer.

### Primitive writes

Fixed-size values written to an `IBufferWriter<byte>` are encoded directly into the span supplied by the writer.

No intermediate managed byte array is required.

### Span reads

Primitive values read from `ReadOnlySpan<byte>` do not require allocations.

Reading a string necessarily allocates the resulting `string`.

### Stream operations

Fixed-size stream operations use small stack-allocated buffers.

Variable-length string encoding uses pooled memory rather than allocating a temporary managed byte array for the encoded representation.

## Use with your own serialization format

Eryri.Buffers does not decide how your application should serialize an object.

You define the format:

```csharp
void WriteMessage(MyMessage message, IBufferWriter<byte> writer)
{
    writer.Write(message.Id);
    writer.Write(message.Timestamp);
    writer.Write(message.Amount);
    writer.Write(message.Name);
}
```

And the corresponding reader:

```csharp
static MyMessage ReadMessage(ReadOnlySpan<byte> data)
{
    var offset = 0;

    offset += data.ReadInt(out var id);
    offset += data.ReadLong(out var timestamp);
    offset += data.ReadDouble(out var amount);
    offset += data.ReadString(out var name);

    return new MyMessage(id, timestamp, amount, name);
}
```

This gives the application complete control over:

* field ordering
* optional values
* versioning
* compatibility
* framing
* validation
* checksums
* compression
* encryption

Eryri.Buffers provides the primitives underneath those decisions.

## What it is not

Eryri.Buffers is **not a general-purpose serializer**.

It does not automatically:

* discover object members
* serialize arbitrary object graphs
* generate schemas
* handle polymorphism
* manage schema evolution
* add type metadata
* perform compression
* perform encryption
* provide message framing

If you need automatic object serialization, use a serialization library designed for that purpose.

If you need complete control over a compact binary format, Eryri.Buffers provides the low-level building blocks.

## Performance

The library is designed for workloads where allocation and buffer management are significant enough to measure.

Performance depends heavily on:

* payload size
* initial buffer capacity
* frequency of buffer growth
* destination type
* string encoding
* stream implementation
* workload concurrency
* CPU and memory characteristics

For this reason, benchmarks should be run against the actual workload rather than relying on a single synthetic number.

## Design goals

Eryri.Buffers aims to provide:

* **Direct access** — write directly into the destination buffer.
* **Low allocation** — reuse storage where practical.
* **Explicit formats** — predictable binary representations.
* **Standard abstractions** — integrate with existing .NET APIs.
* **Small primitives** — compose the library into larger systems.
* **Contiguous buffers** — expose completed data as spans or memory.
* **No hidden serialization model** — the application owns its format.

