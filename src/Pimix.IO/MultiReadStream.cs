using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pimix.IO {
    public class MultiReadStream : Stream {
        public MultiReadStream(List<Stream> streams) {
            Streams = streams;
            Length = Streams.Sum(s => s.Length);
        }

        public List<Stream> Streams { get; set; }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length { get; }
        public override long Position { get; set; }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();

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

            if (Position < 0) {
                throw new ArgumentException(nameof(offset));
            }

            return Position;
        }

        public override void SetLength(long value) =>
            throw new NotSupportedException($"{nameof(SeekableReadStream)} is not writable.");

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException($"{nameof(SeekableReadStream)} is not writable.");
    }
}
