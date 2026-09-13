using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Kifa.IO;

public class VerifiableStream : Stream {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    readonly FileInformation? info;

    byte[]? lastBlock;

    long lastBlockStart = -1;

    Stream stream;

    public VerifiableStream(Stream stream, FileInformation? info) {
        this.stream = stream;
        this.info = info;
    }

    public override bool CanRead => stream.CanRead;

    public override bool CanSeek => stream.CanSeek;

    // Only support read and seek for now.
    public override bool CanWrite => false;

    public override long Length => stream.Length;

    public override long Position { get; set; }

    public override void Flush() {
        stream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count) {
        count = (int) Math.Min(count, Length - Position);
        if (count == 0) {
            return 0;
        }

        var startPosition = Position.RoundDown(FileInformation.BlockSize);
        var endPosition = Math.Min((Position + count).RoundUp(FileInformation.BlockSize), Length);

        Logger.Notice(()
            => $"[{Position}, {Position + count}) -> [{startPosition}, {endPosition})");

        lastBlock ??= new byte[FileInformation.BlockSize];

        var left = count;
        for (var pos = startPosition; pos < endPosition; pos += FileInformation.BlockSize) {
            var bytesToRead = (int) Math.Min(endPosition - pos, FileInformation.BlockSize);
            var bytesRead = 0;
            if (pos == lastBlockStart) {
                Logger.Notice(() => $"[{pos}, {pos + bytesToRead}) skipped");
                bytesRead = bytesToRead;
            } else {
                bool? successful = null;
                var candidates = new Dictionary<(string md5, string sha1, string sha256), int>();
                for (var i = 0; i < 5; ++i) {
                    try {
                        stream.Seek(pos, SeekOrigin.Begin);
                        bytesRead = 0;
                        while (bytesRead < bytesToRead) {
                            var read = stream.Read(lastBlock, bytesRead, bytesToRead - bytesRead);
                            if (read == 0) {
                                break;
                            }

                            bytesRead += read;
                        }

                        if (bytesRead != bytesToRead) {
                            Logger.Warn("Didn't get expected amount of data.");
                            Logger.Warn("Read {0} bytes, should be {1} bytes.", bytesRead,
                                bytesToRead);
                            Thread.Sleep(TimeSpan.FromSeconds(10));
                            continue;
                        }
                    } catch (CryptographicException ex) {
                        Logger.Warn(ex,
                            $"Decrypt error when reading from {Position} to {Position + count}: retrying ({i})...");
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                        continue;
                    } catch (Exception ex) {
                        Logger.Warn(ex, "Failed to read from {0} to {1}:", Position,
                            Position + count);
                        successful = false;
                        break;
                    }


                    var (result, md5, sha1, sha256) = IsBlockValid(lastBlock, 0, bytesRead,
                        (int) (pos / FileInformation.BlockSize));
                    if (result == true) {
                        successful = true;
                        break;
                    }

                    candidates[(md5, sha1, sha256)] =
                        candidates.GetValueOrDefault((md5, sha1, sha256), 0) + 1;

                    if (HasMajority(candidates)) {
                        if (result == false) {
                            Logger.Warn("Block {0} is consistently wrong.",
                                pos / FileInformation.BlockSize);
                            successful = false;
                            break;
                        }

                        if (candidates.Count > 1) {
                            Logger.Debug("Block {0} is not consistently got, but it's fine:",
                                pos / FileInformation.BlockSize);
                            foreach (var candidate in candidates) {
                                Logger.Debug("{0}: {1}", candidate.Key, candidate.Value);
                            }
                        }

                        successful = true;
                        break;
                    }

                    if (result == false) {
                        Logger.Warn("Block {0} may be problematic, retrying ({1})...",
                            pos / FileInformation.BlockSize, i);
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    } else if (candidates.Count > 1) {
                        Logger.Warn("Block {0} has conflicting hashes, retrying ({1})...",
                            pos / FileInformation.BlockSize, i);
                        Thread.Sleep(TimeSpan.FromSeconds(10 * i));
                    }
                }

                if (successful != true) {
                    if (successful == false) {
                        Logger.Error($"Block {pos / FileInformation.BlockSize} is invalid.");
                    } else {
                        if (candidates.Count > 1) {
                            Logger.Warn("Block {0} is too inconsistent:",
                                pos / FileInformation.BlockSize);
                            foreach (var candidate in candidates) {
                                Logger.Warn("{0}: {1}", candidate.Key, candidate.Value);
                            }
                        }
                    }

                    throw new FileCorruptedException(
                        $"Unable to get valid block starting from {pos}");
                }

                Logger.Trace($"[{pos}, {pos + bytesToRead}) got");

                lastBlockStart = pos;
            }

            var copyCount = Math.Min(left, bytesRead - (int) (Position - pos));
            Array.Copy(lastBlock, Position - pos, buffer, offset, copyCount);

            offset += copyCount;
            Position += copyCount;
            left -= copyCount;
        }

        return count;
    }

    bool HasMajority(Dictionary<(string md5, string sha1, string sha256), int> candidates) {
        var totalCount = candidates.Values.Sum();
        if (totalCount == 1) {
            return false;
        }

        return candidates.Values.Any(i => i > totalCount / 2);
    }

    (bool? result, string md5, string sha1, string sha256) IsBlockValid(byte[] buffer, int offset,
        int count, int blockId) {
        bool? result = null;
        string? md5 = null, sha1 = null, sha256 = null;
        using var md5Hasher = MD5.Create();
        using var sha1Hasher = SHA1.Create();
        using var sha256Hasher = SHA256.Create();
        var transformers = new List<Action> {
            () => md5 = md5Hasher.ComputeHash(buffer, offset, count).ToHexString(),
            () => sha1 = sha1Hasher.ComputeHash(buffer, offset, count).ToHexString(),
            () => sha256 = sha256Hasher.ComputeHash(buffer, offset, count).ToHexString(),
        };

        Parallel.ForEach(transformers, new ParallelOptions {
            MaxDegreeOfParallelism = 3
        }, transformer => transformer());

        if (info != null && info.BlockMd5.Count > 0) {
            result = true;
            var expectedMd5 = info.BlockMd5[blockId];

            if (md5 != expectedMd5) {
                Logger.Warn("MD5 mismatch: expected {0}, got {1}", expectedMd5, md5);
                result = false;
            }
        }

        if (info != null && info?.BlockSha1.Count > 0) {
            result ??= true;
            var expectedSha1 = info.BlockSha1[blockId];

            if (sha1 != expectedSha1) {
                Logger.Warn("SHA1 mismatch: expected {0}, got {1}", expectedSha1, sha1);
                result = false;
            }
        }

        if (info != null && info?.BlockSha256.Count > 0) {
            result ??= true;
            var expectedSha256 = info.BlockSha256[blockId];

            if (sha256 != expectedSha256) {
                Logger.Warn("SHA256 mismatch: expected {0}, got {1}", expectedSha256, sha256);
                result = false;
            }
        }

        Logger.Notice(() => $"Block {blockId} ({count} bytes) hash check: MD5={md5}, SHA1={sha1}, SHA256={sha256}, Valid={result}");

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
            if (disposing && stream != null) {
                try {
                    Flush();
                } finally {
                    stream.Dispose();
                }
            }
        } finally {
            stream = null;
            base.Dispose(disposing);
        }
    }
}
