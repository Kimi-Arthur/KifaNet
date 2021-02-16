using System;
using System.IO;
using System.Security.Cryptography;

namespace Kifa.Cryptography {
    public class KifaCryptoStream : Stream {
        readonly bool needBlockAhead;
        byte[] padBuffer;

        long position;
        Stream stream;
        ICryptoTransform transform;

        public KifaCryptoStream(Stream stream, ICryptoTransform transform, long outputLength, bool needBlockAhead) {
            this.stream = stream;
            Length = outputLength;
            this.needBlockAhead = needBlockAhead;
            this.transform = transform;
        }

        int BlockSize => transform.InputBlockSize;

        public override bool CanRead => stream.CanRead;

        public override bool CanSeek => stream.CanSeek;

        public override bool CanWrite => false;

        public override long Length { get; }

        public override long Position {
            get => position;

            set {
                if ((value - 1) / BlockSize != (position - 1) / BlockSize) {
                    padBuffer = null;
                }

                position = value;
            }
        }

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

            var readCount = 0;

            byte[] tmp;

            if (padBuffer != null) {
                var leftOverCount = (int) Math.Min(Position.RoundUp(BlockSize) - Position, count);
                Buffer.BlockCopy(padBuffer, (int) (Position % BlockSize), buffer, offset, leftOverCount);

                Position += leftOverCount;
                readCount += leftOverCount;
                if (readCount == count) {
                    return count;
                }

                if (Position % BlockSize != 0) {
                    throw new Exception("Unexpected");
                }

                var internalToRead = (count - readCount).RoundUp(BlockSize);

                var internalBuffer = new byte[internalToRead];
                var internalReadCount = stream.Read(internalBuffer, 0, internalToRead);

                if (internalReadCount == internalToRead) {
                    tmp = new byte[internalReadCount];
                    transform.TransformBlock(internalBuffer, 0, internalReadCount, tmp, 0);
                } else {
                    tmp = transform.TransformFinalBlock(internalBuffer, 0, internalReadCount);
                }
            } else {
                var internalToRead =
                    (int) ((Position + count - readCount).RoundUp(BlockSize) - Position.RoundDown(BlockSize)) +
                    (needBlockAhead ? BlockSize : 0);
                var internalBuffer = new byte[internalToRead];

                if (stream.CanSeek) {
                    stream.Position = Position.RoundDown(BlockSize);
                }

                var internalReadCount = stream.Read(internalBuffer, 0, internalToRead);

                if (needBlockAhead) {
                    transform.TransformBlock(internalBuffer, 0, BlockSize, new byte[BlockSize], 0);
                }

                if (internalReadCount == internalToRead) {
                    tmp = new byte[internalReadCount - (needBlockAhead ? BlockSize : 0)];
                    transform.TransformBlock(internalBuffer, needBlockAhead ? BlockSize : 0,
                        internalReadCount - (needBlockAhead ? BlockSize : 0), tmp, 0);
                } else {
                    tmp = transform.TransformFinalBlock(internalBuffer, needBlockAhead ? BlockSize : 0,
                        internalReadCount - (needBlockAhead ? BlockSize : 0));
                }
            }

            Buffer.BlockCopy(tmp, (int) (Position % BlockSize), buffer, offset + readCount, count - readCount);

            Position += count - readCount;
            var padCount = tmp.Length % BlockSize == 0 ? BlockSize : tmp.Length % BlockSize;
            padBuffer = new byte[padCount];
            Buffer.BlockCopy(tmp, tmp.Length - padCount, padBuffer, 0, padCount);

            return count;
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
