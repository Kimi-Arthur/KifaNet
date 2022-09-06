using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;

namespace Kifa.IO;

public class ConcatenatedReadStream : Stream {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public List<Stream> Streams { get; set; }

    // streams should be re-readable.
    public ConcatenatedReadStream(List<Stream> streams) {
        Streams = streams;
    }

    public override bool CanRead => true;

    public override bool CanSeek => Streams[0].CanSeek;

    public override bool CanWrite => false;

    public override long Length => Streams.Select(s => s.Length).Sum();

    public override long Position { get; set; }

    (int StreamIndex, long StreamOffset)? streamPosition;

    (int StreamIndex, long StreamOffset) StreamPosition {
        get {
            if (streamPosition != null) {
                return streamPosition.Value;
            }

            var offset = Position;
            for (var i = 0; i < Streams.Count; i++) {
                var length = Streams[i].Length;
                if (offset >= length) {
                    offset -= length;
                } else {
                    return (streamPosition = (i, offset)).Value;
                }
            }

            return (streamPosition = (Streams.Count, 0)).Value;
        }
    }

    public override void Flush() {
        // Intentionally doing nothing.
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

        streamPosition = null;

        if (Position < 0) {
            throw new ArgumentException("Seek position is out of range.", nameof(offset));
        }

        return Position;
    }

    public override void SetLength(long value)
        => throw new NotSupportedException($"{nameof(SeekableReadStream)} is not writable.");

    public override int Read(byte[] buffer, int offset, int count) {
        if (buffer == null) {
            throw new ArgumentNullException(nameof(buffer));
        }

        if (offset < 0) {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        count = (int) Math.Min(count, Length - Position);

        if (buffer.Length - offset < count) {
            throw new ArgumentException();
        }

        // This will trigger preflight of all links.
        if (Position >= Length) {
            return 0;
        }

        var left = count;
        var (index, streamOffset) = StreamPosition;
        for (; index < Streams.Count; index++) {
            var toReadCount = (int) Math.Min(left, Streams[index].Length - streamOffset);
            var readCount = Streams[index].Read(buffer, offset, toReadCount);
            if (toReadCount != readCount) {
                throw new FileCorruptedException(
                    $"Expected to read {toReadCount} bytes, but only got {readCount} bytes from Streams[{index}]");
            }

            offset += readCount;
            left -= readCount;

            if (left == 0) {
                if (readCount + streamOffset == Streams[index].Length) {
                    // Finished at the end of the current stream.
                    streamPosition = (index + 1, 0);
                } else {
                    streamPosition = (index, streamOffset + readCount);
                }

                return count;
            }

            streamOffset = 0;
        }

        throw new FileCorruptedException("Read beyond end of streams.");
    }

    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException($"{nameof(SeekableReadStream)} is not writable.");
}
