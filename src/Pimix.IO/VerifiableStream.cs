using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using NLog;

namespace Pimix.IO
{
    public class VerfiableStream : Stream
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        
        static HashAlgorithm MD5Hasher = new MD5CryptoServiceProvider();
        
        static HashAlgorithm SHA1Hasher = new SHA1CryptoServiceProvider();
        
        static HashAlgorithm SHA256Hasher = new SHA256CryptoServiceProvider();

        Stream stream;

        FileInformation info;

        public override bool CanRead
            => stream.CanRead;

        public override bool CanSeek
            => stream.CanSeek;

        // Only support read and seek for now.
        public override bool CanWrite
            => false;

        public override long Length
            => info.Size.Value;

        public override long Position { get; set; }

        public VerfiableStream(Stream stream, FileInformation info)
        {
            this.stream = stream;
            this.info = info;
        }

        public override void Flush()
        {
            stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            count = (int)Math.Min(count, Length - Position);
            if (count == 0)
                return 0;

            long startPosition = Position.RoundDown(FileInformation.BlockSize);
            long endPosition = Math.Min((Position + count).RoundUp(FileInformation.BlockSize), Length);

            logger.Debug("Read {0} bytes from {1}.", count, Position);
            logger.Debug("Effective block: {0} to {1}.", startPosition, endPosition);
            byte[] blockRead = new byte[FileInformation.BlockSize];

            int left = count;
            for (long pos = startPosition; pos < endPosition; pos += FileInformation.BlockSize)
            {
                int bytesToRead = (int)Math.Min(endPosition - pos, (long)FileInformation.BlockSize);
                int bytesRead = 0;

                bool successful = false;
                for (int i = 0; i < 5; ++i)
                {
                    stream.Seek(pos, SeekOrigin.Begin);
                    bytesRead = stream.Read(blockRead, 0, bytesToRead);
                    if (bytesRead == bytesToRead && isBlockValid(blockRead, 0, bytesRead, (int)(pos / FileInformation.BlockSize))) {
                        successful = true;
                        break;
                    } else {
                        logger.Warn("Block {0} is problematic, retrying ({1})...", pos / FileInformation.BlockSize, i);
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }
                }

                if (!successful) {
                    throw new Exception($"Unable to get valid block starting from {pos}");
                }

                int copyCount = Math.Min(left, bytesRead - (int)(Position - pos));
                Array.Copy(blockRead, Position - pos, buffer, offset, copyCount);

                offset += copyCount;
                Position += copyCount;
                left -= copyCount;
            }

            return count;
        }

        bool isBlockValid(byte[] buffer, int offset, int count, int blockId) {
            bool result = true;

            if (info.BlockMD5 != null)
            {
                string expectedMd5 = info.BlockMD5[blockId];
                string md5 = MD5Hasher.ComputeHash(buffer, offset, count).ToHexString();

                if (md5 != expectedMd5)
                {
                    logger.Warn("MD5 mismatch: expected {0}, got {1}", expectedMd5, md5);
                    result = false;
                }
            }

            if (info.BlockSHA1 != null)
            {
                string expectedSha1 = info.BlockSHA1[blockId];
                string sha1 = SHA1Hasher.ComputeHash(buffer, offset, count).ToHexString();

                if (sha1 != expectedSha1)
                {
                    logger.Warn("SHA1 mismatch: expected {0}, got {1}", expectedSha1, sha1);
                    result = false;
                }
            }

            if (info.BlockSHA256 != null)
            {
                string expectedSha256 = info.BlockSHA256[blockId];
                string sha256 = SHA256Hasher.ComputeHash(buffer, offset, count).ToHexString();

                if (sha256 != expectedSha256)
                {
                    logger.Warn("SHA256 mismatch: expected {0}, got {1}", expectedSha256, sha256);
                    result = false;
                }
            }

            return result;
        }

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
                if (disposing && stream != null)
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
