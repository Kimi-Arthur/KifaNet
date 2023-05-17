using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kifa.IO;

public class MultiReadStream : Stream {
    public MultiReadStream(List<Stream> streams) {
        this.streams = streams;
        streamCount = streams.Count;
        lengths = this.streams.Select(s => s.Length).ToList();
        offsets = new List<long> {
            0
        };

        foreach (var t in lengths) {
            offsets.Add(offsets.Last() + t);
        }

        Length = offsets.Last();
    }

    readonly List<Stream> streams;
    readonly List<long> lengths;
    readonly List<long> offsets;
    int streamCount;

    int currentStream = -1;
    long positionInCurrentStream = -1;

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length { get; }
    public override long Position { get; set; }

    public override int Read(byte[] buffer, int offset, int count) {
        count = (int) Math.Min(count, Length - Position);
        if (count == 0) {
            return 0;
        }

        UpdateCurrentPosition();

        var toRead = count;
        for (var s = currentStream; s < streams.Count; s++) {
            streams[s].Seek(positionInCurrentStream, SeekOrigin.Begin);
            var read = streams[s].Read(buffer, offset, toRead);
            toRead -= read;
            offset += read;
            if (streams[s].Position == streams[s].Length) {
                currentStream++;
                positionInCurrentStream = 0;
            } else {
                positionInCurrentStream = streams[s].Position;
            }

            if (toRead == 0) {
                break;
            }
        }

        Position += count;

        return count;
    }

    void UpdateCurrentPosition() {
        if (currentStream != -1) {
            return;
        }

        int l = 0, r = offsets.Count;
        while (r - l > 1) {
            var x = (l + r) / 2;
            if (Position < offsets[x]) {
                r = x;
            } else {
                l = x;
            }
        }

        currentStream = l;
        positionInCurrentStream = Position - offsets[l];
    }

    public override void Flush() {
        // Intentionally doing nothing.
    }

    public override long Seek(long offset, SeekOrigin origin) {
        switch (origin) {
            case SeekOrigin.Begin:
                if (Position != offset) {
                    positionInCurrentStream = currentStream = -1;
                }

                Position = offset;
                break;
            case SeekOrigin.Current:
                if (offset != 0) {
                    positionInCurrentStream = currentStream = -1;
                }

                Position += offset;
                break;
            case SeekOrigin.End:
                if (Position != Length + offset) {
                    positionInCurrentStream = currentStream = -1;
                }

                Position = Length + offset;
                break;
        }

        if (Position < 0) {
            throw new ArgumentException(nameof(offset));
        }

        return Position;
    }

    public override void SetLength(long value)
        => throw new NotSupportedException($"{nameof(SeekableReadStream)} is not writable.");

    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException($"{nameof(SeekableReadStream)} is not writable.");

    protected override void Dispose(bool disposing) {
        foreach (var stream in streams) {
            stream.Dispose();
        }
    }
}
