using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using NLog;

namespace Pimix.IO {
    public class VerifiableStream : Stream {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static readonly HashAlgorithm MD5Hasher = new MD5CryptoServiceProvider();

        static readonly HashAlgorithm SHA1Hasher = new SHA1CryptoServiceProvider();

        static readonly HashAlgorithm SHA256Hasher = new SHA256CryptoServiceProvider();

        Stream stream;

        readonly FileInformation info;

        public override bool CanRead => stream.CanRead;

        public override bool CanSeek => stream.CanSeek;

        // Only support read and seek for now.
        public override bool CanWrite => false;

        public override long Length => stream.Length;

        public override long Position { get; set; }

        public VerifiableStream(Stream stream, FileInformation info) {
            this.stream = stream;
            this.info = info;
        }

        public override void Flush() {
            stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count) {
            count = (int) Math.Min(count, Length - Position);
            if (count == 0)
                return 0;

            var startPosition = Position.RoundDown(FileInformation.BlockSize);
            var endPosition =
                Math.Min((Position + count).RoundUp(FileInformation.BlockSize), Length);

            logger.Debug("Read {0} bytes from {1}.", count, Position);
            logger.Debug("Effective block: {0} to {1}.", startPosition, endPosition);
            var blockRead = new byte[FileInformation.BlockSize];

            var left = count;
            for (var pos = startPosition; pos < endPosition; pos += FileInformation.BlockSize) {
                var bytesToRead = (int) Math.Min(endPosition - pos, FileInformation.BlockSize);
                var bytesRead = 0;

                var successful = false;
                var candidates = new Dictionary<(string md5, string sha1, string sha256), int>();
                for (var i = 0; i < 5; ++i) {
                    stream.Seek(pos, SeekOrigin.Begin);
                    bytesRead = stream.Read(blockRead, 0, bytesToRead);
                    if (bytesRead == bytesToRead) {
                        var result = IsBlockValid(blockRead, 0, bytesRead,
                            (int) (pos / FileInformation.BlockSize));
                        if (result.result == true) {
                            successful = true;
                            break;
                        }

                        if (result.result == false) {
                            logger.Warn("Block {0} is problematic, retrying ({1})...",
                                pos / FileInformation.BlockSize, i);
                            Thread.Sleep(TimeSpan.FromSeconds(10));
                        } else {
                            candidates[(result.md5, result.sha1, result.sha256)] =
                                candidates.GetValueOrDefault(
                                    (result.md5, result.sha1, result.sha256), 0) + 1;
                            if (HasMajority(candidates)) {
                                if (candidates.Count > 1) {
                                    logger.Debug(
                                        "Block {0} is not consistently got, but it's fine:",
                                        pos / FileInformation.BlockSize);
                                    foreach (var candidate in candidates)
                                        logger.Debug("{0}: {1}", candidate.Key, candidate.Value);
                                }

                                successful = true;
                                break;
                            }

                            Thread.Sleep(TimeSpan.FromSeconds(10));
                        }
                    } else {
                        logger.Warn(
                            "Block {0} is problematic (unexpected read length {1}, should be {2}), retrying ({3})...",
                            pos / FileInformation.BlockSize, bytesRead, bytesToRead, i);
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }
                }

                if (!successful) {
                    if (candidates.Count > 1) {
                        logger.Warn("Block {0} is too inconsistent:",
                            pos / FileInformation.BlockSize);
                        foreach (var candidate in candidates)
                            logger.Warn("{0}: {1}", candidate.Key, candidate.Value);
                    }

                    throw new Exception($"Unable to get valid block starting from {pos}");
                }

                var copyCount = Math.Min(left, bytesRead - (int) (Position - pos));
                Array.Copy(blockRead, Position - pos, buffer, offset, copyCount);

                offset += copyCount;
                Position += copyCount;
                left -= copyCount;
            }

            return count;
        }

        bool HasMajority(Dictionary<(string md5, string sha1, string sha256), int> candidates) {
            var totalCount = candidates.Values.Sum();
            if (totalCount == 1) return false;

            return candidates.Values.Any(i => i > totalCount / 2);
        }

        (bool? result, string md5, string sha1, string sha256) IsBlockValid(byte[] buffer,
            int offset, int count, int blockId) {
            bool? result = null;

            var md5 = MD5Hasher.ComputeHash(buffer, offset, count).ToHexString();
            if (info.BlockMD5 != null) {
                result = true;
                var expectedMd5 = info.BlockMD5[blockId];

                if (md5 != expectedMd5) {
                    logger.Warn("MD5 mismatch: expected {0}, got {1}", expectedMd5, md5);
                    result = false;
                }
            }

            var sha1 = SHA1Hasher.ComputeHash(buffer, offset, count).ToHexString();
            if (info.BlockSHA1 != null) {
                result = result ?? true;
                var expectedSha1 = info.BlockSHA1[blockId];

                if (sha1 != expectedSha1) {
                    logger.Warn("SHA1 mismatch: expected {0}, got {1}", expectedSha1, sha1);
                    result = false;
                }
            }

            var sha256 = SHA256Hasher.ComputeHash(buffer, offset, count).ToHexString();

            if (info.BlockSHA256 != null) {
                result = result ?? true;
                var expectedSha256 = info.BlockSHA256[blockId];

                if (sha256 != expectedSha256) {
                    logger.Warn("SHA256 mismatch: expected {0}, got {1}", expectedSha256, sha256);
                    result = false;
                }
            }

            return (result, md5, sha1, sha256);
        }

        public override long Seek(long offset, SeekOrigin origin) {
            switch (origin) {
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

        public override void SetLength(long value) {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count) {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing) {
            try {
                if (disposing && stream != null)
                    try {
                        Flush();
                    } finally {
                        stream.Dispose();
                    }
            } finally {
                stream = null;
                base.Dispose(disposing);
            }
        }
    }
}
