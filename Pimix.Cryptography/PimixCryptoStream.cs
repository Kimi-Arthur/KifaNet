using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Pimix;

namespace Pimix.Cryptography
{
    public class PimixCryptoStream : Stream
    {
        Stream stream;
        bool needBlockAhead;
        long length;
        long streamOffset;
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
            => length;

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
                    padBuffer = null;
                }

                position = value;
            }
        }

        long InternalPosition
        {
            get
            {
                return stream.Position - streamOffset;
            }
            set
            {
                stream.Position = value + streamOffset;
            }
        }

        public PimixCryptoStream(Stream stream, ICryptoTransform transform, long length, bool needBlockAhead, long streamOffset = 0)
        {
            this.stream = stream;
            this.length = length;
            this.needBlockAhead = needBlockAhead;
            this.streamOffset = streamOffset;
            this.transform = transform;
        }

        public override void Flush()
        {
            stream.Flush();
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
                InternalPosition = Position + (needBlockAhead ? BlockSize : 0);
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

                InternalPosition = Position.RoundDown(BlockSize);
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
    }
}
