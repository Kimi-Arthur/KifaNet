namespace CG.Web.MegaApiClient
{
    using System;
    using System.IO;

    class StreamWithLength : Stream
    {
        protected readonly long streamLength;

        private readonly Stream stream;

        public StreamWithLength(Stream stream, long streamLength)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            this.stream = stream;
            this.streamLength = streamLength;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return this.streamLength; }
        }

        public override long Position { get; set; } = 0;

        public override int Read(byte[] buffer, int offset, int count)
        {
            count = (int)Math.Min(count, Length - Position);
            int readLength = stream.Read(buffer, offset, count);
            while (readLength < count)
            {
                readLength += stream.Read(buffer, offset + readLength, count - readLength);
            }

            Position += readLength;

            return readLength;
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
