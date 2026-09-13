using System;
using System.IO;
using System.Security.Cryptography;
using NLog;

namespace Kifa.Cryptography;

public class KifaCryptoStream : Stream {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    const int BlockSize = 16;

    readonly Stream stream;
    readonly Aes aes;
    readonly bool isDecoding;
    long position;

    public KifaCryptoStream(Stream stream, Aes aes, long outputLength, bool isDecoding) {
        this.stream = stream;
        this.aes = aes;
        this.isDecoding = isDecoding;
        Length = outputLength;
    }

    public KifaCryptoStream(Stream stream, ICryptoTransform transform, long outputLength,
        bool needBlockAhead, IDisposable? algorithm = null) {
        this.stream = stream;
        this.isDecoding = needBlockAhead;
        Length = outputLength;
        if (algorithm is Aes a) {
            this.aes = a;
        } else {
            throw new NotSupportedException("Only Aes algorithm is supported by KifaCryptoStream.");
        }
    }

    public override bool CanRead => stream.CanRead;

    public override bool CanSeek => stream.CanSeek;

    public override bool CanWrite => false;

    public override long Length { get; }

    public override long Position {
        get => position;
        set => position = value;
    }

    public override void Flush() {
        stream?.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count) {
        if (buffer == null) {
            throw new ArgumentNullException(nameof(buffer));
        }

        if (offset < 0) {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (buffer.Length - offset < count) {
            throw new ArgumentException();
        }

        count = (int) Math.Min(count, Length - Position);

        if (count == 0) {
            return 0;
        }

        return isDecoding
            ? ReadDecrypted(buffer, offset, count)
            : ReadEncrypted(buffer, offset, count);
    }

    int ReadDecrypted(byte[] buffer, int offset, int count) {
        var startBlock = Position.RoundDown(BlockSize);
        var endBlock = (Position + count).RoundUp(BlockSize);
        var totalAlignedBytes = (int) (endBlock - startBlock);

        Logger.Diagnostic(() => $"KifaCryptoStream Decrypt: Position [{Position}..{Position + count}), block range [{startBlock}..{endBlock}), bytes={totalAlignedBytes}");

        if (stream.CanSeek) {
            stream.Position = startBlock;
        }

        var cipherBuffer = new byte[totalAlignedBytes];
        var totalRead = ReadInternal(cipherBuffer, 0, totalAlignedBytes);
        if (totalRead < totalAlignedBytes) {
            Array.Clear(cipherBuffer, totalRead, totalAlignedBytes - totalRead);
        }

        var plainBuffer = new byte[totalAlignedBytes];
        aes.DecryptEcb(cipherBuffer, plainBuffer, PaddingMode.None);

        var copyOffset = (int) (Position - startBlock);
        Buffer.BlockCopy(plainBuffer, copyOffset, buffer, offset, count);

        Position += count;
        return count;
    }

    int ReadEncrypted(byte[] buffer, int offset, int count) {
        var startBlock = Position.RoundDown(BlockSize);
        var endBlock = (Position + count).RoundUp(BlockSize);
        var totalAlignedBytes = (int) (endBlock - startBlock);

        Logger.Diagnostic(() => $"KifaCryptoStream Encrypt: Position [{Position}..{Position + count}), block range [{startBlock}..{endBlock}), bytes={totalAlignedBytes}, Length={Length}");

        var lastBlockStart = Length - BlockSize;
        var plainBuffer = new byte[totalAlignedBytes];

        if (endBlock <= lastBlockStart) {
            // All requested blocks are full plaintext blocks before the final padded block.
            if (stream.CanSeek) {
                stream.Position = startBlock;
            }

            ReadInternal(plainBuffer, 0, totalAlignedBytes);
        } else if (startBlock >= lastBlockStart) {
            // Only the final padded block is requested.
            var rawPlaintextSize = stream.CanSeek ? stream.Length : Length - BlockSize;
            var lastBlockPlainCount =
                (int) Math.Max(0, Math.Min(BlockSize, rawPlaintextSize - lastBlockStart));
            var padCount = BlockSize - lastBlockPlainCount;

            if (stream.CanSeek) {
                stream.Position = lastBlockStart;
            }

            if (lastBlockPlainCount > 0) {
                ReadInternal(plainBuffer, 0, lastBlockPlainCount);
            }

            // ANSIX923 padding: padCount - 1 zeros followed by padCount byte
            Array.Clear(plainBuffer, lastBlockPlainCount, padCount - 1);
            plainBuffer[BlockSize - 1] = (byte) padCount;
        } else {
            // Span across regular blocks and the final padded block.
            var nonFinalBytes = (int) (lastBlockStart - startBlock);
            if (stream.CanSeek) {
                stream.Position = startBlock;
            }

            ReadInternal(plainBuffer, 0, nonFinalBytes);

            var rawPlaintextSize = stream.CanSeek ? stream.Length : Length - BlockSize;
            var lastBlockPlainCount =
                (int) Math.Max(0, Math.Min(BlockSize, rawPlaintextSize - lastBlockStart));
            var padCount = BlockSize - lastBlockPlainCount;

            if (lastBlockPlainCount > 0) {
                ReadInternal(plainBuffer, nonFinalBytes, lastBlockPlainCount);
            }

            Array.Clear(plainBuffer, nonFinalBytes + lastBlockPlainCount, padCount - 1);
            plainBuffer[totalAlignedBytes - 1] = (byte) padCount;
        }

        var cipherBuffer = new byte[totalAlignedBytes];
        aes.EncryptEcb(plainBuffer, cipherBuffer, PaddingMode.None);

        var copyOffset = (int) (Position - startBlock);
        Buffer.BlockCopy(cipherBuffer, copyOffset, buffer, offset, count);

        Position += count;
        return count;
    }

    int ReadInternal(byte[] internalBuffer, int offset, int count) {
        var totalRead = 0;
        while (totalRead < count) {
            var read = stream.Read(internalBuffer, offset + totalRead, count - totalRead);
            if (read == 0) {
                break;
            }

            totalRead += read;
        }

        return totalRead;
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

        return Position;
    }

    public override void SetLength(long value) {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count) {
        throw new NotImplementedException();
    }

    protected override void Dispose(bool disposing) {
        try {
            if (disposing) {
                Flush();
                stream?.Dispose();
            }
        } finally {
            base.Dispose(disposing);
        }
    }
}
