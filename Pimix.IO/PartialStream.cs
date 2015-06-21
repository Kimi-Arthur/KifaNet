using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pimix.IO
{
    public class PartialStream : Stream
    {
        Stream stream;
        long ignoreBefore;
        long ignoreAfter;

        public override bool CanRead
            => stream.CanRead;

        public override bool CanSeek
            => stream.CanSeek;

        // Only support read and seek for now.
        public override bool CanWrite
            => false;

        public override long Length
            => stream.Length - ignoreBefore - ignoreAfter;

        public override long Position
        {
            get
            {
                return stream.Position + ignoreBefore;
            }

            set
            {
                if (value >= 0 && value < Length)
                    stream.Position = value + ignoreBefore;
                else
                {
                    // throw what?
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
            }
        }

        public PartialStream(Stream stream, long ignoreBefore = 0, long ignoreAfter = 0)
        {
            this.stream = stream;
            this.ignoreBefore = ignoreBefore;
            this.ignoreAfter = ignoreAfter;
            if (Position < 0 || Position >= Length)
            {
                Position = 0;
            }
        }

        public override void Flush()
        {
            stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
            => stream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    return Position;
                case SeekOrigin.Current:
                    Position += offset;
                    return Position;
                case SeekOrigin.End:
                    Position = Length + offset;
                    return Position;
                default:
                    return Position;
            }
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
                    try
                    {
                        Flush();
                    }
                    finally
                    {
                        stream.Dispose();
                    }
                }
            }
            finally
            {
                stream = null;
                base.Dispose(disposing);
            }
        }
    }
}
