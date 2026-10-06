using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Eryri.Buffers.Tests;

public abstract class ArrayPoolBufferWriterTests<T> where T : IEquatable<T>
{
    [Test]
    public void ArrayBufferWriter_Ctor()
    {
        {
            using var output = new ArrayPoolBufferWriter<T>();
            output.FreeCapacity.Should().Be(256);
            output.Capacity.Should().Be(256);
            output.WrittenCount.Should().Be(0);
            output.WrittenSpan.Length.Should().Be(0);
            output.WrittenMemory.Length.Should().Be(0);
        }

        {
            using var output = new ArrayPoolBufferWriter<T>(200);
            output.FreeCapacity.Should().BeGreaterThanOrEqualTo(200);
            output.Capacity.Should().BeGreaterThanOrEqualTo(200);
            output.WrittenCount.Should().Be(0);
            output.WrittenSpan.Length.Should().Be(0);
            output.WrittenMemory.Length.Should().Be(0);
        }
    }

    [Test]
    public void Invalid_Ctor()
    {
        FluentActions.Invoking(() => new ArrayPoolBufferWriter<T>(0))
            .Should().Throw<ArgumentException>();

        FluentActions.Invoking(() => new ArrayPoolBufferWriter<T>(-1))
            .Should().Throw<ArgumentException>();

        FluentActions.Invoking(() => new ArrayPoolBufferWriter<T>(int.MaxValue))
            .Should().Throw<OutOfMemoryException>();
    }

    [Test]
    public void Clear()
    {
        using var output = new ArrayPoolBufferWriter<T>();
        int previousAvailable = output.FreeCapacity;
        WriteData(output, 2);
        output.FreeCapacity.Should().BeLessThan(previousAvailable);
        output.WrittenCount.Should().BeGreaterThan(0);
        output.WrittenSpan.Length.Should().BeGreaterThan(0);
        output.WrittenMemory.Length.Should().BeGreaterThan(0);
        output.WrittenSpan.SequenceEqual(output.WrittenMemory.Span).Should().BeTrue();

        ReadOnlyMemory<T> transientMemory = output.WrittenMemory;
        ReadOnlySpan<T> transientSpan = output.WrittenSpan;
        T t0 = transientMemory.Span[0];
        T t1 = transientSpan[1];
        t0.Should().NotBe(default(T));
        t1.Should().NotBe(default(T));
        output.Clear();
        transientMemory.Span[0].Should().Be(default(T));
        transientSpan[1].Should().Be(default(T));

        output.WrittenCount.Should().Be(0);
        ReadOnlySpan<T>.Empty.SequenceEqual(output.WrittenSpan).Should().BeTrue();
        ReadOnlyMemory<T>.Empty.Span.SequenceEqual(output.WrittenMemory.Span).Should().BeTrue();
        output.FreeCapacity.Should().Be(previousAvailable);
    }

    [Test]
    public void ResetWrittenCount()
    {
        using var output = new ArrayPoolBufferWriter<T>(256);
        int previousAvailable = output.FreeCapacity;
        WriteData(output, 2);
        output.FreeCapacity.Should().BeLessThan(previousAvailable);
        output.WrittenCount.Should().BeGreaterThan(0);
        ReadOnlySpan<T>.Empty.SequenceEqual(output.WrittenSpan).Should().BeFalse();
        ReadOnlyMemory<T>.Empty.Span.SequenceEqual(output.WrittenMemory.Span).Should().BeFalse();
        output.WrittenSpan.SequenceEqual(output.WrittenMemory.Span).Should().BeTrue();

        ReadOnlyMemory<T> transientMemory = output.WrittenMemory;
        ReadOnlySpan<T> transientSpan = output.WrittenSpan;
        T t0 = transientMemory.Span[0];
        T t1 = transientSpan[1];
        t0.Should().NotBe(default!);
        t1.Should().NotBe(default!);
        output.ResetWrittenCount();
        transientMemory.Span[0].Should().Be(t0);
        transientSpan[1].Should().Be(t1);

        output.WrittenCount.Should().Be(0);
        ReadOnlySpan<T>.Empty.SequenceEqual(output.WrittenSpan).Should().BeTrue();
        ReadOnlyMemory<T>.Empty.Span.SequenceEqual(output.WrittenMemory.Span).Should().BeTrue();
        output.FreeCapacity.Should().Be(previousAvailable);
    }

    [Test]
    public void Advance()
    {
        {
            using var output = new ArrayPoolBufferWriter<T>();
            int capacity = output.Capacity;
            output.FreeCapacity.Should().Be(capacity);
            output.Advance(output.FreeCapacity);
            output.WrittenCount.Should().Be(capacity);
            output.FreeCapacity.Should().Be(0);
        }

        {
            using var output = new ArrayPoolBufferWriter<T>();
            output.Advance(output.Capacity);
            output.WrittenCount.Should().Be(output.Capacity);
            output.FreeCapacity.Should().Be(0);
            int previousCapacity = output.Capacity;
            Span<T> _ = output.GetSpan();
            output.Capacity.Should().BeGreaterThan(previousCapacity);
        }

        {
            using var output = new ArrayPoolBufferWriter<T>(256);
            WriteData(output, 2);
            ReadOnlyMemory<T> previousMemory = output.WrittenMemory;
            ReadOnlySpan<T> previousSpan = output.WrittenSpan;
            previousSpan.SequenceEqual(previousMemory.Span).Should().BeTrue();
            output.Advance(10);
            previousMemory.Span.SequenceEqual(output.WrittenMemory.Span).Should().BeFalse();
            previousSpan.SequenceEqual(output.WrittenSpan).Should().BeFalse();
            output.WrittenSpan.SequenceEqual(output.WrittenMemory.Span).Should().BeTrue();
        }

        {
            using var output = new ArrayPoolBufferWriter<T>();
            _ = output.GetSpan(20);
            WriteData(output, 10);
            ReadOnlyMemory<T> previousMemory = output.WrittenMemory;
            ReadOnlySpan<T> previousSpan = output.WrittenSpan;
            previousSpan.SequenceEqual(previousMemory.Span).Should().BeTrue();
            FluentActions.Invoking(() => output.Advance(247))
                .Should().Throw<InvalidOperationException>();
            output.Advance(10);
            previousMemory.Span.SequenceEqual(output.WrittenMemory.Span).Should().BeFalse();
            previousSpan.SequenceEqual(output.WrittenSpan).Should().BeFalse();
            output.WrittenSpan.SequenceEqual(output.WrittenMemory.Span).Should().BeTrue();
        }
    }

    [Test]
    public void AdvanceZero()
    {
        using var output = new ArrayPoolBufferWriter<T>();
        WriteData(output, 2);
        output.WrittenCount.Should().Be(2);
        ReadOnlyMemory<T> previousMemory = output.WrittenMemory;
        ReadOnlySpan<T> previousSpan = output.WrittenSpan;
        previousSpan.SequenceEqual(previousMemory.Span).Should().BeTrue();
        output.Advance(0);
        output.WrittenCount.Should().Be(2);
        previousMemory.Span.SequenceEqual(output.WrittenMemory.Span).Should().BeTrue();
        previousSpan.SequenceEqual(output.WrittenSpan).Should().BeTrue();
        output.WrittenSpan.SequenceEqual(output.WrittenMemory.Span).Should().BeTrue();
    }

    [Test]
    public void InvalidAdvance()
    {
        {
            using var output = new ArrayPoolBufferWriter<T>();
            FluentActions.Invoking(() => output.Advance(-1))
                .Should().Throw<ArgumentException>();
            FluentActions.Invoking(() => output.Advance(output.Capacity + 1))
                .Should().Throw<InvalidOperationException>();
        }

        {
            using var output = new ArrayPoolBufferWriter<T>();
            WriteData(output, 100);
            FluentActions.Invoking(() => output.Advance(output.FreeCapacity + 1))
                .Should().Throw<InvalidOperationException>();
        }
    }

    [Test]
    public void GetSpan_DefaultCtor()
    {
        using var output = new ArrayPoolBufferWriter<T>();
        Span<T> span = output.GetSpan();
        span.Length.Should().Be(256);
    }

    [Theory]
    [TestCaseSource(nameof(SizeHints))]
    public void GetSpan_DefaultCtor_WithSizeHint(int sizeHint)
    {
        using var output = new ArrayPoolBufferWriter<T>();
        Span<T> span = output.GetSpan(sizeHint);
        span.Length.Should().BeGreaterThanOrEqualTo(sizeHint <= 256 ? 256 : sizeHint);
    }

    [Test]
    public void GetSpan_InitSizeCtor()
    {
        using var output = new ArrayPoolBufferWriter<T>(100);
        Span<T> span = output.GetSpan();
        span.Length.Should().BeGreaterThanOrEqualTo(100);
    }

    [Theory]
    [TestCaseSource(nameof(SizeHints))]
    public void GetSpan_InitSizeCtor_WithSizeHint(int sizeHint)
    {
        {
            using var output = new ArrayPoolBufferWriter<T>(256);
            Span<T> span = output.GetSpan(sizeHint);
            span.Length.Should().BeGreaterThanOrEqualTo(sizeHint <= 256 ? 256 : sizeHint + 256);
        }

        {
            using var output = new ArrayPoolBufferWriter<T>(1000);
            Span<T> span = output.GetSpan(sizeHint);
            span.Length.Should().BeGreaterThanOrEqualTo(sizeHint <= 1000 ? 1000 : sizeHint + 1000);
        }
    }

    [Test]
    public void GetMemory_DefaultCtor()
    {
        using var output = new ArrayPoolBufferWriter<T>();
        Memory<T> memory = output.GetMemory();
        memory.Length.Should().Be(256);
    }

    [Theory]
    [TestCaseSource(nameof(SizeHints))]
    public void GetMemory_DefaultCtor_WithSizeHint(int sizeHint)
    {
        using var output = new ArrayPoolBufferWriter<T>();
        Memory<T> memory = output.GetMemory(sizeHint);
        memory.Length.Should().BeGreaterThanOrEqualTo(sizeHint <= 256 ? 256 : sizeHint);
    }

    [Test]
    public void GetMemory_ExceedMaximumBufferSize_WithSmallStartingSize()
    {
        using var output = new ArrayPoolBufferWriter<T>(256);
        FluentActions.Invoking(() => output.GetMemory(int.MaxValue))
            .Should().Throw<OverflowException>();
    }

    [Test]
    public void GetMemory_InitSizeCtor()
    {
        using var output = new ArrayPoolBufferWriter<T>(100);
        Memory<T> memory = output.GetMemory();
        memory.Length.Should().BeGreaterThanOrEqualTo(100);
    }

    [Theory]
    [TestCaseSource(nameof(SizeHints))]
    public void GetMemory_InitSizeCtor_WithSizeHint(int sizeHint)
    {
        {
            using var output = new ArrayPoolBufferWriter<T>(256);
            Memory<T> memory = output.GetMemory(sizeHint);
            memory.Length.Should().BeGreaterThanOrEqualTo(sizeHint <= 256 ? 256 : sizeHint + 256);
        }

        {
            using var output = new ArrayPoolBufferWriter<T>(1000);
            Memory<T> memory = output.GetMemory(sizeHint);
            memory.Length.Should().BeGreaterThanOrEqualTo(sizeHint <= 1000 ? 1000 : sizeHint + 1000);
        }
    }

    [Test]
    public void GetMemoryAndSpan()
    {
        {
            using var output = new ArrayPoolBufferWriter<T>();
            WriteData(output, 2);
            Span<T> span = output.GetSpan();
            Memory<T> memory = output.GetMemory();
            Span<T> memorySpan = memory.Span;
            span.Length.Should().BeGreaterThan(0);
            memorySpan.Length.Should().BeGreaterThan(0);
            memorySpan.Length.Should().Be(span.Length);
            for (int i = 0; i < span.Length; i++)
            {
                span[i].Should().Be(memorySpan[i]);
            }
        }

        {
            using var output = new ArrayPoolBufferWriter<T>();
            WriteData(output, 2);
            ReadOnlyMemory<T> writtenSoFarMemory = output.WrittenMemory;
            ReadOnlySpan<T> writtenSoFar = output.WrittenSpan;
            writtenSoFarMemory.Span.SequenceEqual(writtenSoFar).Should().BeTrue();
            int previousAvailable = output.FreeCapacity;
            Span<T> span = output.GetSpan(500);
            span.Length.Should().BeGreaterThanOrEqualTo(500);
            output.FreeCapacity.Should().BeGreaterThanOrEqualTo(500);
            output.FreeCapacity.Should().BeGreaterThan(previousAvailable);

            output.WrittenCount.Should().Be(writtenSoFar.Length);
            writtenSoFar.SequenceEqual(span.Slice(0, output.WrittenCount)).Should().BeTrue();

            Memory<T> memory = output.GetMemory();
            Span<T> memorySpan = memory.Span;
            span.Length.Should().BeGreaterThanOrEqualTo(500);
            memorySpan.Length.Should().BeGreaterThanOrEqualTo(500);
            memorySpan.Length.Should().Be(span.Length);
            for (int i = 0; i < span.Length; i++)
            {
                span[i].Should().Be(default(T));
                memorySpan[i].Should().Be(default(T));
            }

            memory = output.GetMemory(500);
            memorySpan = memory.Span;
            memorySpan.Length.Should().BeGreaterThanOrEqualTo(500);
            memorySpan.Length.Should().Be(span.Length);
            for (int i = 0; i < memorySpan.Length; i++)
            {
                memorySpan[i].Should().Be(default(T));
            }
        }
    }

    [Test]
    public void GetSpanShouldAtleastDoubleWhenGrowing()
    {
        using var output = new ArrayPoolBufferWriter<T>(256);
        WriteData(output, 100);
        int previousAvailable = output.FreeCapacity;

        _ = output.GetSpan(previousAvailable);
        output.FreeCapacity.Should().Be(previousAvailable);

        _ = output.GetSpan(previousAvailable + 1);
        output.FreeCapacity.Should().BeGreaterThanOrEqualTo(previousAvailable * 2);
    }

    [Test]
    public void GetSpanOnlyGrowsAboveThreshold()
    {
        {
            using var output = new ArrayPoolBufferWriter<T>();
            _ = output.GetSpan();
            int previousAvailable = output.FreeCapacity;

            for (int i = 0; i < 10; i++)
            {
                _ = output.GetSpan();
                output.FreeCapacity.Should().Be(previousAvailable);
            }
        }

        {
            using var output = new ArrayPoolBufferWriter<T>();
            _ = output.GetSpan(10);
            int previousAvailable = output.FreeCapacity;

            for (int i = 0; i < 10; i++)
            {
                _ = output.GetSpan(previousAvailable);
                output.FreeCapacity.Should().Be(previousAvailable);
            }
        }
    }

    [Test]
    public void InvalidGetMemoryAndSpan()
    {
        using var output = new ArrayPoolBufferWriter<T>();
        WriteData(output, 2);
        FluentActions.Invoking(() => output.GetSpan(-1))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => output.GetMemory(-1))
            .Should().Throw<ArgumentException>();
    }

    [Test]
    public void MultipleCallsToGetSpan()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            return;
        }

        using var output = new ArrayPoolBufferWriter<T>(300);
        MemoryMarshal.TryGetArray(output.GetMemory(), out ArraySegment<T> array)
            .Should().BeTrue();
        GCHandle pinnedArray = GCHandle.Alloc(array.Array, GCHandleType.Pinned);
        try
        {
            int previousAvailable = output.FreeCapacity;
            using (new AssertionScope())
            {
                previousAvailable.Should().BeGreaterThanOrEqualTo(300);
                output.Capacity.Should().BeGreaterThanOrEqualTo(300);
                output.Capacity.Should().Be(previousAvailable);
            }

            Span<T> span = output.GetSpan();
            span.Length.Should().BeGreaterThanOrEqualTo(previousAvailable);
            span.Length.Should().BeGreaterThanOrEqualTo(256);
            Span<T> newSpan = output.GetSpan();
            newSpan.Length.Should().Be(span.Length);
            Unsafe.ByteOffset(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetReference(newSpan))
                .Should().Be(0);
            output.GetSpan().Length.Should().Be(span.Length);
        }
        finally
        {
            pinnedArray.Free();
        }
    }

    protected abstract void WriteData(IBufferWriter<T> bufferWriter, int numBytes);

    public static IEnumerable<object[]> SizeHints
    {
        get
        {
            return new List<object[]>
            {
                new object[] { 0 },
                new object[] { 1 },
                new object[] { 2 },
                new object[] { 3 },
                new object[] { 99 },
                new object[] { 100 },
                new object[] { 101 },
                new object[] { 255 },
                new object[] { 256 },
                new object[] { 257 },
                new object[] { 1000 },
                new object[] { 2000 },
            };
        }
    }
}
