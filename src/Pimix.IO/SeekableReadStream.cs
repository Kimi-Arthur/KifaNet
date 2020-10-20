using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Pimix.IO {
    public class SeekableReadStream : Stream {
        public delegate int Reader(byte[] buffer, int bufferOffset = 0, long offset = 0, int count = -1);

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        readonly Reader reader;
        readonly int maxChunkSize;
        readonly int threadCount;

        public SeekableReadStream(long length, Reader reader, int maxChunkSize = int.MaxValue, int threadCount = 1) {
            Length = length;
            this.reader = reader;
            this.maxChunkSize = maxChunkSize;
            this.threadCount = threadCount;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length { get; }

        public override long Position { get; set; }

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

        public override int Read(byte[] buffer, int offset, int count) {
            if (buffer == null) {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0) {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            count = (int) Math.Min(count, Length - Position);

            if (buffer.Length - offset < count) {
                throw new ArgumentException();
            }

            if (Position >= Length) {
                return 0;
            }

            Parallel.For(0, (count - 1) / maxChunkSize + 1, new ParallelOptions {MaxDegreeOfParallelism = threadCount},
                i => {
                    Thread.Sleep(TimeSpan.FromSeconds(i * 4));

                    var chunkOffset = i * maxChunkSize;
                    var chunkSize = Math.Min(maxChunkSize, count - chunkOffset);
                    Retry.Run(() => {
                        var readCount = reader(buffer, offset + chunkOffset, Position + chunkOffset, chunkSize);
                        if (readCount != chunkSize) {
                            throw new Exception($"Expected {chunkSize}, only got {readCount}");
                        }
                    }, (ex, index) => {
                        if (index >= 5) {
                            throw ex;
                        }

                        logger.Warn(ex, $"Internal failure getting {chunkSize} bytes from {Position + chunkOffset}.");
                        Thread.Sleep(TimeSpan.FromSeconds(5 * index));
                    });
                });

            Position += count;
            return count;
        }

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException($"{nameof(SeekableReadStream)} is not writable.");
    }
}
