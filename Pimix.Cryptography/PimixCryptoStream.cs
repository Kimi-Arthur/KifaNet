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
        long length;
        long streamOffset;
        ICryptoTransform transform;
        byte[] padBuffer;
        bool prePadded;

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
                stream.Position = streamOffset + streamOffset;
            }
        }

        public PimixCryptoStream(Stream stream, ICryptoTransform transform, long length, long streamOffset = 0)
        {
            this.stream = stream;
            this.length = length;
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

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (buffer.Length - offset < count)
                throw new ArgumentException();

            if (count % BlockSize != 0 && count >= 2 * BlockSize)
                throw new ArgumentException();

            if (Position % BlockSize != 0)
                throw new ArgumentException();

            count = (int)Math.Min(count, Length - Position);

            if (count == 0)
                return 0;

            int readCount = 0;

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

                int internalToRead = count - readCount;
                // No need to add a block since the current block is actually in pad buffer in
                // transform object.
                internalToRead = internalToRead.RoundUp(BlockSize);

                byte[] internalBuffer = new byte[internalToRead];
                InternalPosition = Position + BlockSize;
                int internalReadCount = stream.Read(internalBuffer, 0, internalToRead);

                if (internalReadCount % BlockSize != 0)
                    throw new Exception("Unexpected");

                byte[] tmp;

                if (internalReadCount == internalToRead)
                {
                    tmp = new byte[internalReadCount];
                    transform.TransformBlock(internalBuffer, 0, internalReadCount, tmp, 0);
                }
                else
                {
                    tmp = transform.TransformFinalBlock(internalBuffer, 0, internalReadCount);
                }

                Buffer.BlockCopy(tmp, 0, buffer, offset + readCount, count - readCount);

                Position += count - readCount;
                padBuffer = new byte[BlockSize];
                Buffer.BlockCopy(tmp, tmp.Length.RoundDown(BlockSize), padBuffer, 0, tmp.Length - tmp.Length.RoundDown(BlockSize));

                return count;
            }
            else
            {
                InternalPosition = Position.RoundDown(BlockSize);
                int internalToRead = (int) ((Position + count).RoundUp(BlockSize) - InternalPosition + BlockSize);

                byte[] internalBuffer = new byte[internalToRead];
                InternalPosition = Position + BlockSize;
                int internalReadCount = stream.Read(internalBuffer, 0, internalToRead);

                // We don't care about the result for this since it's either empty or
                // from somewhere of no concern.
                transform.TransformBlock(internalBuffer, 0, BlockSize, null, 0);

                byte[] tmp;

                if (internalReadCount == internalToRead)
                {
                    tmp = new byte[internalReadCount - BlockSize];
                    transform.TransformBlock(internalBuffer, BlockSize, internalReadCount - BlockSize, tmp, 0);
                }
                else
                {
                    tmp = transform.TransformFinalBlock(internalBuffer, BlockSize, internalReadCount - BlockSize);
                }


                Buffer.BlockCopy(tmp, 0, buffer, offset + readCount, count - readCount);

                padBuffer = new byte[BlockSize];
                Buffer.BlockCopy(tmp, tmp.Length.RoundDown(BlockSize), padBuffer, 0, tmp.Length - tmp.Length.RoundDown(BlockSize));
                return count;
            }

            byte[] buf = new byte[count];
            int readCount = stream.Read(buf, 0, count);
            if (readCount != count)
            {
                buf = transform.TransformFinalBlock(buf, 0, readCount);
                Buffer.BlockCopy(buf, 0, buffer, offset, buf.Length);
                return buf.Length;
            }
            else
            {
                return transform.TransformBlock(buf, 0, count, buffer, offset);
            }
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
