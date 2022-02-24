using System;
using System.IO;

namespace CG.Web.MegaApiClient; 

class StreamWithLength : Stream {
    readonly Stream stream;
    protected readonly long streamLength;

    public StreamWithLength(Stream stream, long streamLength) {
        if (stream == null) {
            throw new ArgumentNullException("stream");
        }

        this.stream = stream;
        this.streamLength = streamLength;
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => streamLength;

    public override long Position { get; set; }

    public override int Read(byte[] buffer, int offset, int count) {
        count = (int) Math.Min(count, Length - Position);
        var readLength = stream.Read(buffer, offset, count);
        while (readLength < count) {
            readLength += stream.Read(buffer, offset + readLength, count - readLength);
        }

        Position += readLength;

        return readLength;
    }

    public override void Flush() {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
        => throw new NotSupportedException();

    public override void SetLength(long value) {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count) {
        throw new NotSupportedException();
    }
}