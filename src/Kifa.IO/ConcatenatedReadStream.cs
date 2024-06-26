﻿using System;
using System.Collections.Generic;
using System.IO;
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

    int countedStreams;
    long tentativeLength;

    // Optional parameter threshold meaning if the length is known to be longer than that, we don't
    // care about the actual value.
    long GetTentativeLength(long threshold = long.MaxValue) {
        if (threshold == long.MaxValue) {
            Logger.Debug("Full length is calculated.");
        }

        if (tentativeLength > threshold) {
            return tentativeLength;
        }

        while (countedStreams < Streams.Count && tentativeLength <= threshold) {
            tentativeLength += Streams[countedStreams++].Length;
        }

        return tentativeLength;
    }

    public override long Length => GetTentativeLength();

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

        count = (int) Math.Min(count, GetTentativeLength(count + Position) - Position);

        if (buffer.Length - offset < count) {
            throw new ArgumentException();
        }

        // This will trigger preflight of all links.
        if (Position >= GetTentativeLength(Position)) {
            return 0;
        }

        Logger.Trace($"Reading {count} bytes from {Position}...");

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

                Position += count;
                return count;
            }

            streamOffset = 0;
        }

        throw new FileCorruptedException(
            $"Read beyond end of streams. {left}/{count} bytes to read.");
    }

    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException($"{nameof(SeekableReadStream)} is not writable.");
}
