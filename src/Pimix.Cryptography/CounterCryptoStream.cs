using System;
using System.IO;
using System.Security.Cryptography;

namespace Pimix.Cryptography {
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

            var counter = initialCounter.Add(Position / blockSize);

            var bufferOffset = offset;

            while (Position < stream.Position) {
                var transformed = transform.TransformFinalBlock(counter, 0, counter.Length);

                do {
                    buffer[bufferOffset++] ^= transformed[Position++ % blockSize];
                } while (Position < stream.Position && Position % blockSize > 0);

                counter = counter.Add(1);
            }

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
}
