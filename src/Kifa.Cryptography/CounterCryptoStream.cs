using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Kifa.Cryptography;

public class CounterCryptoStream : Stream {
    readonly byte[] initialCounter;

    Stream stream;
    ICryptoTransform transform;

    public CounterCryptoStream(Stream stream, ICryptoTransform transform, long outputLength,
        byte[] initialCounter) {
        this.stream = stream;
        Length = outputLength;
        this.initialCounter = initialCounter;
        this.transform = transform;
        blockSize = transform.InputBlockSize;
    }

    readonly int blockSize;

    public override bool CanRead => stream.CanRead;

    public override bool CanSeek => stream.CanSeek;

    public override bool CanWrite => false;

    public override long Length { get; }

    public override long Position { get; set; }

    public override void Flush() {
        stream?.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count) {
        if (buffer == null) {
            throw new ArgumentNullException(nameof(buffer));
        }

        if (offset < 0) {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (buffer.Length - offset < count) {
            throw new ArgumentException();
        }

        count = (int) Math.Min(count, Length - Position);

        if (count == 0) {
            return 0;
        }

        if (stream.CanSeek) {
            stream.Position = Position;
        }

        var readCount = stream.Read(buffer, offset, count);

        var counter = initialCounter.ToArray();
        counter.Add(Position / blockSize);

        var counterCount = (stream.Position.RoundDown(blockSize) - Position.RoundUp(blockSize)) /
                           blockSize;
        counterCount += stream.Position % blockSize > 0 ? 1 : 0;
        counterCount += Position % blockSize > 0 ? 1 : 0;

        var counters = new byte[counterCount * blockSize];
        for (var i = 0; i < counterCount; i++) {
            Buffer.BlockCopy(counter, 0, counters, i * blockSize, counter.Length);
            counter.Add(1);
        }

        var transformed = transform.TransformFinalBlock(counters, 0, counters.Length);

        var originalPosition = Position;
        var transformedOffset = Position % blockSize;
        var streamPosition = stream.Position;
        Parallel.For(0, streamPosition - Position, new ParallelOptions {
            MaxDegreeOfParallelism = 8
        }, i => { buffer[offset + i] ^= transformed[i + transformedOffset]; });
        Position = streamPosition;

        return readCount;
    }

    public override long Seek(long offset, SeekOrigin origin) {
        switch (origin) {
            case SeekOrigin.Begin:
                Position = offset;
                break;
            case SeekOrigin.Current:
                Position += offset;
                break;
            case SeekOrigin.End:
                Position = Length + offset;
                break;
        }

        return Position;
    }

    public override void SetLength(long value) {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count) {
        throw new NotImplementedException();
    }

    protected override void Dispose(bool disposing) {
        try {
            if (disposing) {
                Flush();
                stream?.Dispose();
                transform?.Dispose();
            }
        } finally {
            stream = null;
            transform = null;
            base.Dispose(disposing);
        }
    }
}
