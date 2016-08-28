using System;
using System.IO;
using System.Security.Cryptography;

namespace Pimix.Cryptography
{
    public class PimixCryptoStream : Stream
    {
        Stream stream;
        bool needBlockAhead;
        long outputLength;
        ICryptoTransform transform;
        byte[] padBuffer;

        int BlockSize
            => transform.InputBlockSize;

        public override bool CanRead
            => stream.CanRead;

        public override bool CanSeek
            => stream.CanSeek;

        public override bool CanWrite
            => false;

        public override long Length
            => outputLength;

        long position;
        public override long Position
        {
            get
            {
                return position;
            }

            set
            {
                if ((value - 1) / BlockSize != (position - 1) / BlockSize)
                {
                    // The block is changed.
                    padBuffer = null;
                }

                position = value;
            }
        }

        public PimixCryptoStream(Stream stream, ICryptoTransform transform, long outputLength, bool needBlockAhead)
        {
            this.stream = stream;
            this.outputLength = outputLength;
            this.needBlockAhead = needBlockAhead;
            this.transform = transform;
        }

        public override void Flush()
        {
            stream?.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (buffer.Length - offset < count)
                throw new ArgumentException();

            count = (int)Math.Min(count, Length - Position);

            if (count == 0)
                return 0;

            int readCount = 0;

            byte[] tmp;

            if (padBuffer != null)
            {
                int leftOverCount = (int)Math.Min(Position.RoundUp(BlockSize) - Position, count);
                Buffer.BlockCopy(padBuffer, (int)(Position % BlockSize), buffer, offset, leftOverCount);

                Position += leftOverCount;
                readCount += leftOverCount;
                if (readCount == count)
                    return count;

                if (Position % BlockSize != 0)
                    throw new Exception("Unexpected");

                int internalToRead = (count - readCount).RoundUp(BlockSize);

                byte[] internalBuffer = new byte[internalToRead];
                int internalReadCount = stream.Read(internalBuffer, 0, internalToRead);

                if (internalReadCount == internalToRead)
                {
                    tmp = new byte[internalReadCount];
                    transform.TransformBlock(internalBuffer, 0, internalReadCount, tmp, 0);
                }
                else
                {
                    tmp = transform.TransformFinalBlock(internalBuffer, 0, internalReadCount);
                }

            }
            else
            {
                int internalToRead = (int)((Position + count - readCount).RoundUp(BlockSize) - Position.RoundDown(BlockSize)) + (needBlockAhead ? BlockSize : 0);
                byte[] internalBuffer = new byte[internalToRead];

                if (stream.CanSeek)
                {
                    stream.Position = Position.RoundDown(BlockSize);
                }

                int internalReadCount = stream.Read(internalBuffer, 0, internalToRead);

                if (needBlockAhead)
                {
                    // We don't care about the result for this since it's either empty or
                    // from somewhere of no concern.
                    transform.TransformBlock(internalBuffer, 0, BlockSize, new byte[BlockSize], 0);
                }

                if (internalReadCount == internalToRead)
                {
                    tmp = new byte[internalReadCount - (needBlockAhead ? BlockSize : 0)];
                    transform.TransformBlock(internalBuffer, needBlockAhead ? BlockSize : 0, internalReadCount - (needBlockAhead ? BlockSize : 0), tmp, 0);
                }
                else
                {
                    tmp = transform.TransformFinalBlock(internalBuffer, needBlockAhead ? BlockSize : 0, internalReadCount - (needBlockAhead ? BlockSize : 0));
                }

            }

            Buffer.BlockCopy(tmp, (int)(Position % BlockSize), buffer, offset + readCount, count - readCount);

            Position += count - readCount;
            int padCount = tmp.Length % BlockSize == 0 ? BlockSize : tmp.Length % BlockSize;
            padBuffer = new byte[padCount];
            Buffer.BlockCopy(tmp, tmp.Length - padCount, padBuffer, 0, padCount);

            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
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

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    Flush();
                    stream?.Dispose();
                    transform?.Dispose();
                }
            }
            finally
            {
                stream = null;
                transform = null;
                base.Dispose(disposing);
            }
        }
    }
}
