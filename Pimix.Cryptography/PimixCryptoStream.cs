using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Pimix.Cryptography
{
    public class PimixCryptoStream : Stream
    {
        Stream stream;
        long length;
        long streamOffset;
        ICryptoTransform transform;

        public override bool CanRead
            => stream.CanRead;

        public override bool CanSeek
            => stream.CanSeek;

        public override bool CanWrite
            => false;

        public override long Length
            => length;

        public override long Position
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

        public PimixCryptoStream(Stream stream, ICryptoTransform transform, long length, long streamOffset = 0)
        {
            this.stream = stream;
            this.length = length;
            this.streamOffset = streamOffset;
            this.transform = transform;
            this.Position = 0;
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

            if (count % transform.InputBlockSize != 0 && count >= 2 * transform.InputBlockSize)
                throw new ArgumentException();

            if (Position % transform.InputBlockSize != 0)
                throw new ArgumentException();

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
