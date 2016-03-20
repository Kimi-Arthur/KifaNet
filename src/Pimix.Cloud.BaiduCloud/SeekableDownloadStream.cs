using System;
using System.IO;
using System.Threading;

namespace Pimix.Cloud.BaiduCloud
{
    class SeekableDownloadStream : Stream
    {
        public delegate int Downloader(byte[] buffer, int bufferOffset = 0, long offset = 0, int count = -1);

        Downloader downloader;

        bool isOpen = true;

        public override bool CanRead
            => isOpen;

        public override bool CanSeek
            => isOpen;

        public override bool CanWrite
            => false;

        long length;
        public override long Length => length;

        public override long Position { get; set; }

        public SeekableDownloadStream(long length, Downloader downloader)
        {
            this.length = length;
            this.downloader = downloader;
        }

        public override void Flush()
        {
            if (!isOpen)
                throw new ObjectDisposedException(null);

            // Intentionally doing nothing.
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!isOpen)
                throw new ObjectDisposedException(null);

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
            throw new NotSupportedException("The Baidu download stream is not writable.");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!isOpen)
                throw new ObjectDisposedException(null);

            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (buffer.Length - offset < count)
                throw new ArgumentException();

            if (Position >= Length)
            {
                return 0;
            }

            count = (int)Math.Min(count, Length - Position);

            bool done = false;
            int readCount = 0;
            while (!done)
            {
                try
                {
                    readCount = downloader(buffer, offset, Position, count);
                    done = readCount == count;
                    if (!done)
                    {
                        Console.Error.WriteLine("Didn't get expected amount of data.");
                        Console.Error.WriteLine($"Responses contains {readCount} bytes, should be {count} bytes.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed once when downloading (from {Position} to {Position + count}):");
                    Console.WriteLine("Exception:");
                    Console.WriteLine(ex);
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                }
            }

            Position += readCount;

            return readCount;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("The Baidu download stream is not writable.");
        }
    }
}
