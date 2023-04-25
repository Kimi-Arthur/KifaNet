using System;
using System.IO;

namespace Kifa.IO;

public class PatchedStream : Stream {
    Stream stream;

    public PatchedStream(Stream stream) {
        this.stream = stream;
    }

    public long IgnoreBefore { get; set; } = 0;

    public long IgnoreAfter { get; set; } = 0;

    public byte[] BufferBefore { get; set; } = new byte[0];

    public byte[] BufferAfter { get; set; } = new byte[0];

    public override bool CanRead => stream.CanRead;

    public override bool CanSeek => stream.CanSeek;

    // Only support read and seek for now.
    public override bool CanWrite => false;

    public override long Length
        => stream.Length - IgnoreBefore - IgnoreAfter + BufferBefore.LongLength +
           BufferAfter.LongLength;

    public override long Position { get; set; }

    public override void Flush() {
        stream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count) {
        count = (int) Math.Min(count, Length - Position);
        if (count == 0) {
            return 0;
        }

        var readCount = 0;

        if (Position < BufferBefore.Length) {
            var beforeCount = (int) Math.Min(count - readCount, BufferBefore.Length - Position);
            Array.Copy(BufferBefore, Position, buffer, offset + readCount, beforeCount);
            Position += beforeCount;
            readCount += beforeCount;

            if (readCount == count) {
                return count;
            }
        }

        if (Position < Length - BufferAfter.Length) {
            var streamCount = (int) Math.Min(count - readCount,
                Length - BufferAfter.Length - Position);
            stream.Seek(Position - BufferBefore.Length + IgnoreBefore, SeekOrigin.Begin);
            stream.ReadExactly(buffer, offset + readCount, streamCount);
            Position += streamCount;
            readCount += streamCount;

            if (readCount == count) {
                return count;
            }
        }

        var afterCount = count - readCount;
        Array.Copy(BufferAfter,
            Position - BufferBefore.Length - stream.Length + IgnoreAfter + IgnoreBefore, buffer,
            offset + readCount, afterCount);
        Position += afterCount;

        return count;
    }

    public override long Seek(long offset, SeekOrigin origin) {
        switch (origin) {
            case SeekOrigin.Begin:
                Position = offset;
                return Position;
            case SeekOrigin.Current:
                Position += offset;
                return Position;
            case SeekOrigin.End:
                Position = Length + offset;
                return Position;
            default:
                return Position;
        }
    }

    public override void SetLength(long value) {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count) {
        throw new NotImplementedException();
    }

    protected override void Dispose(bool disposing) {
        try {
            if (disposing && stream != null) {
                try {
                    Flush();
                } finally {
                    stream.Dispose();
                }
            }
        } finally {
            stream = null;
            base.Dispose(disposing);
        }
    }
}
