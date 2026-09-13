using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using NLog;

namespace Kifa.Cryptography;

public class CounterCryptoStream : Stream {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    const int BlockSize = 16;

    readonly byte[] initialCounter;
    readonly Stream stream;
    readonly Aes aes;
    long position;

    public CounterCryptoStream(Stream stream, Aes aes, long outputLength, byte[] initialCounter) {
        this.stream = stream;
        this.aes = aes;
        Length = outputLength;
        this.initialCounter = initialCounter;
    }

    public CounterCryptoStream(Stream stream, ICryptoTransform transform, long outputLength,
        byte[] initialCounter, IDisposable? algorithm = null) {
        this.stream = stream;
        Length = outputLength;
        this.initialCounter = initialCounter;
        if (algorithm is Aes a) {
            this.aes = a;
        } else {
            throw new NotSupportedException(
                "Only Aes algorithm is supported by CounterCryptoStream.");
        }
    }

    public override bool CanRead => stream.CanRead;

    public override bool CanSeek => stream.CanSeek;

    public override bool CanWrite => false;

    public override long Length { get; }

    public override long Position {
        get => position;
        set => position = value;
    }

    public override void Flush() {
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

        var totalRead = 0;
        while (totalRead < count) {
            var read = stream.Read(buffer, offset + totalRead, count - totalRead);
            if (read == 0) {
                break;
            }

            totalRead += read;
        }

        var counter = initialCounter.ToArray();
        counter.Add(Position / BlockSize);

        var endPos = Position + totalRead;
        var counterCount = (endPos.RoundDown(BlockSize) - Position.RoundUp(BlockSize)) / BlockSize;
        counterCount += endPos % BlockSize > 0 ? 1 : 0;
        counterCount += Position % BlockSize > 0 ? 1 : 0;

        var counters = new byte[counterCount * BlockSize];
        for (var i = 0; i < counterCount; i++) {
            Buffer.BlockCopy(counter, 0, counters, i * BlockSize, counter.Length);
            counter.Add(1);
        }

        Logger.Notice(() => $"CounterCryptoStream: Position [{Position}..{Position + totalRead}), counterCount={counterCount}");

        var transformed = new byte[counters.Length];
        aes.EncryptEcb(counters, transformed, PaddingMode.None);

        var transformedOffset = (int) (Position % BlockSize);
        Parallel.For(0, totalRead, new ParallelOptions {
            MaxDegreeOfParallelism = 8
        }, i => { buffer[offset + i] ^= transformed[i + transformedOffset]; });

        Position += totalRead;
        return totalRead;
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
            }
        } finally {
            base.Dispose(disposing);
        }
    }
}
