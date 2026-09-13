using System;
using System.IO;
using System.Security.Cryptography;

namespace Kifa.Cryptography;

public class KifaCryptoStream : Stream {
    readonly bool needBlockAhead;

    // Keep reference to the underlying SymmetricAlgorithm so native OpenSSL cipher contexts are not GC'd prematurely during streaming.
    readonly IDisposable? algorithm;
    byte[] padBuffer;

    long position;
    Stream stream;
    ICryptoTransform transform;

    public KifaCryptoStream(Stream stream, ICryptoTransform transform, long outputLength,
        bool needBlockAhead, IDisposable? algorithm = null) {
        this.stream = stream;
        Length = outputLength;
        this.needBlockAhead = needBlockAhead;
        this.transform = transform;
        this.algorithm = algorithm;
    }

    int BlockSize => transform.InputBlockSize;

    public override bool CanRead => stream.CanRead;

    public override bool CanSeek => stream.CanSeek;

    public override bool CanWrite => false;

    public override long Length { get; }

    public override long Position {
        get => position;

        set {
            if ((value - 1) / BlockSize != (position - 1) / BlockSize) {
                padBuffer = null;
            }

            position = value;
        }
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

        var readCount = 0;

        byte[] tmp;

        if (padBuffer != null) {
            var leftOverCount = (int) Math.Min(Position.RoundUp(BlockSize) - Position, count);
            Buffer.BlockCopy(padBuffer, (int) (Position % BlockSize), buffer, offset,
                leftOverCount);

            Position += leftOverCount;
            readCount += leftOverCount;
            if (readCount == count) {
                return count;
            }

            if (Position % BlockSize != 0) {
                throw new Exception("Unexpected");
            }

            var internalToRead = (count - readCount).RoundUp(BlockSize);

            var internalBuffer = new byte[internalToRead];
            if (stream.CanSeek) {
                // Ensure underlying seekable stream is aligned with the requested block boundary plus any lookahead block.
                stream.Position = Position.RoundDown(BlockSize) + (needBlockAhead ? BlockSize : 0);
            }

            var internalReadCount = ReadInternal(internalBuffer, 0, internalToRead);

            if (internalReadCount == internalToRead) {
                tmp = new byte[internalReadCount];
                TransformBlockChunked(internalBuffer, 0, internalReadCount, tmp, 0);
            } else {
                tmp = transform.TransformFinalBlock(internalBuffer, 0, internalReadCount);
            }
        } else {
            var internalToRead =
                (int) ((Position + count - readCount).RoundUp(BlockSize) -
                       Position.RoundDown(BlockSize)) + (needBlockAhead ? BlockSize : 0);
            var internalBuffer = new byte[internalToRead];

            if (stream.CanSeek) {
                stream.Position = Position.RoundDown(BlockSize);
            }

            var internalReadCount = ReadInternal(internalBuffer, 0, internalToRead);

            if (needBlockAhead) {
                TransformBlockChunked(internalBuffer, 0, BlockSize, new byte[BlockSize], 0);
            }

            if (internalReadCount == internalToRead) {
                tmp = new byte[internalReadCount - (needBlockAhead ? BlockSize : 0)];
                TransformBlockChunked(internalBuffer, needBlockAhead ? BlockSize : 0,
                    internalReadCount - (needBlockAhead ? BlockSize : 0), tmp, 0);
            } else {
                tmp = transform.TransformFinalBlock(internalBuffer, needBlockAhead ? BlockSize : 0,
                    internalReadCount - (needBlockAhead ? BlockSize : 0));
            }
        }

        Buffer.BlockCopy(tmp, (int) (Position % BlockSize), buffer, offset + readCount,
            count - readCount);

        Position += count - readCount;
        var padCount = tmp.Length % BlockSize == 0 ? BlockSize : tmp.Length % BlockSize;
        padBuffer = new byte[padCount];
        Buffer.BlockCopy(tmp, tmp.Length - padCount, padBuffer, 0, padCount);

        return count;
    }

    // Maximum block size per TransformBlock call. Breaking large multi-megabyte transfers into 64KB chunks
    // prevents native OpenSSL / SIMD buffer boundary corruption on Android/ARM64 and improves CPU cache locality.
    const int CryptoChunkSize = 64 * 1024;

    void TransformBlockChunked(byte[] inputBuffer, int inputOffset, int inputCount,
        byte[] outputBuffer, int outputOffset) {
        var processed = 0;
        while (processed < inputCount) {
            var chunkSize = Math.Min(CryptoChunkSize, inputCount - processed);
            transform.TransformBlock(inputBuffer, inputOffset + processed, chunkSize, outputBuffer,
                outputOffset + processed);
            processed += chunkSize;
        }
    }

    // Reads until count is satisfied or true EOF is reached. Single Stream.Read() calls on network/storage streams
    // (especially on mobile/Termux) can return partial chunks; treating partial reads as EOF prematurely triggers
    // TransformFinalBlock, which appends padding mid-stream and corrupts all subsequent cipher blocks.
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
                transform?.Dispose();
                algorithm?.Dispose();
            }
        } finally {
            stream = null;
            transform = null;
            base.Dispose(disposing);
        }
    }
}
