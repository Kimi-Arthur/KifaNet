using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Kifa.IO;
using Xunit;

namespace Kifa.Cryptography.Tests;

public class KifaCryptoStreamTests {
    readonly Aes aesAlgorithm = Aes.Create();

    public KifaCryptoStreamTests() {
        aesAlgorithm.Key =
            "C7C37D56DD70FD6258BDD01AED083C88432EC27536DF9328D6329382183DB795".ParseHexString();
    }

    readonly List<(string rawFile, long rawSize, string rawHash, string encryptedFile, long
        encryptedSize, string encryptedHash)> data = new() {
        ("data-1.raw.bin", 65536,
            "0A43E6858977A39B861420FA31877030A0E683F1E25FFCF6A42098E6CB4C4948", "data-1.aes.bin",
            65552, "2222C7B3D3D1896636DDC0642F8CC2F882D23CECCE1D2FEE7678B5B3587A3163"),
        ("data-2.raw.bin", 13659,
            "8FFB7A1DFF0EDF9A670AAD939828357FB017D9C6526648BF2D31292DA983DFDF", "data-2.aes.bin",
            13664, "E1223699AFBDFBB5252D7CCEA23A40BFCE8DD4834A53E52A75C778BEC3C72706")
    };

    [Fact]
    public void KifaCryptoStreamDecryptionReadBasicTest() {
        foreach (var (rawFile, rawSize, rawHash, encryptedFile, encryptedSize, encryptedHash) in
                 data) {
            using var stream =
                new KifaCryptoStream(File.OpenRead(encryptedFile), aesAlgorithm, rawSize, true);
            var info = FileInformation.GetInformation(stream,
                FileProperties.Sha256 | FileProperties.Size | FileProperties.SliceMd5);
            Assert.Equal(rawSize, info.Size);
            Assert.Equal(rawHash, info.Sha256);

            foreach (var b in new List<int> {
                         8,
                         11,
                         12,
                         16,
                         33,
                         100
                     }) {
                stream.Seek(0, SeekOrigin.Begin);
                var output = new MemoryStream();
                stream.CopyTo(output, b);

                info = FileInformation.GetInformation(output,
                    FileProperties.Sha256 | FileProperties.Size | FileProperties.SliceMd5);
                Assert.Equal(rawSize, info.Size);
                Assert.Equal(rawHash, info.Sha256);
            }
        }
    }

    [Fact]
    public void KifaCryptoStreamDecryptionReadIncompleteEndTest() {
        foreach (var (rawFile, rawSize, rawHash, encryptedFile, encryptedSize, encryptedHash) in
                 data) {
            using var stream =
                new KifaCryptoStream(File.OpenRead(encryptedFile), aesAlgorithm, rawSize, true);
            var baseStream = new MemoryStream();
            stream.CopyTo(baseStream);

            foreach (var b in new List<int> {
                         8,
                         11,
                         12,
                         16,
                         33,
                         100
                     }) {
                stream.Seek(0, SeekOrigin.Begin);
                stream.Seek(-b, SeekOrigin.End);
                baseStream.Seek(-b, SeekOrigin.End);
                for (var i = 0; i < b; i++) {
                    Assert.Equal(baseStream.ReadByte(), stream.ReadByte());
                }
            }
        }
    }

    [Fact]
    public void KifaCryptoStreamEncryptionReadBasicTest() {
        foreach (var (rawFile, rawSize, rawHash, encryptedFile, encryptedSize, encryptedHash) in
                 data) {
            using var stream =
                new KifaCryptoStream(File.OpenRead(rawFile), aesAlgorithm, encryptedSize, false);
            var info = FileInformation.GetInformation(stream,
                FileProperties.Sha256 | FileProperties.Size | FileProperties.SliceMd5);
            Assert.Equal(encryptedSize, info.Size);
            Assert.Equal(encryptedHash, info.Sha256);

            foreach (var b in new List<int> {
                         8,
                         11,
                         12,
                         16,
                         33,
                         100
                     }) {
                stream.Seek(0, SeekOrigin.Begin);
                var output = new MemoryStream();
                stream.CopyTo(output, b);

                info = FileInformation.GetInformation(output,
                    FileProperties.Sha256 | FileProperties.Size | FileProperties.SliceMd5);
                Assert.Equal(encryptedSize, info.Size);
                Assert.Equal(encryptedHash, info.Sha256);
            }
        }
    }

    [Fact]
    public void KifaCryptoStreamPartialReadTest() {
        foreach (var (rawFile, rawSize, rawHash, encryptedFile, encryptedSize, encryptedHash) in
                 data) {
            using var encryptStream =
                new KifaCryptoStream(new PartialReadStream(File.OpenRead(rawFile), 1024), aesAlgorithm, encryptedSize, false);
            var info = FileInformation.GetInformation(encryptStream,
                FileProperties.Sha256 | FileProperties.Size | FileProperties.SliceMd5);
            Assert.Equal(encryptedSize, info.Size);
            Assert.Equal(encryptedHash, info.Sha256);

            using var decryptStream =
                new KifaCryptoStream(new PartialReadStream(File.OpenRead(encryptedFile), 1024), aesAlgorithm, rawSize, true);
            var decryptInfo = FileInformation.GetInformation(decryptStream,
                FileProperties.Sha256 | FileProperties.Size | FileProperties.SliceMd5);
            Assert.Equal(rawSize, decryptInfo.Size);
            Assert.Equal(rawHash, decryptInfo.Sha256);
        }
    }

    [Fact]
    public void BlockChunkReadTest() {
        foreach (var (rawFile, rawSize, rawHash, encryptedFile, encryptedSize, encryptedHash) in
                 data) {
            var rawBytes = File.ReadAllBytes(rawFile);
            using var decryptStream =
                new KifaCryptoStream(File.OpenRead(encryptedFile), aesAlgorithm, rawSize, true);

            // Read in 100-byte chunks
            var chunkSize = 100;
            var buffer = new byte[chunkSize];
            var totalRead = 0;
            while (totalRead < rawSize) {
                var toRead = (int) Math.Min(chunkSize, rawSize - totalRead);
                var read = decryptStream.Read(buffer, 0, toRead);
                Assert.Equal(toRead, read);
                for (int i = 0; i < read; i++) {
                    if (rawBytes[totalRead + i] != buffer[i]) {
                        throw new Exception($"Mismatch in {rawFile} at {totalRead + i}: expected {rawBytes[totalRead + i]}, got {buffer[i]}");
                    }
                }
                totalRead += read;
            }

            // Now test seeking and reading
            for (var offset = 0; offset < rawSize; offset += 500) {
                decryptStream.Seek(offset, SeekOrigin.Begin);
                var toRead = (int) Math.Min(chunkSize, rawSize - offset);
                var read = decryptStream.Read(buffer, 0, toRead);
                Assert.Equal(toRead, read);
                for (int i = 0; i < read; i++) {
                    Assert.Equal(rawBytes[offset + i], buffer[i]);
                }
            }
        }
    }
}

class PartialReadStream(Stream baseStream, int maxChunkSize) : Stream {
    public override bool CanRead => baseStream.CanRead;
    public override bool CanSeek => baseStream.CanSeek;
    public override bool CanWrite => false;
    public override long Length => baseStream.Length;
    public override long Position {
        get => baseStream.Position;
        set => baseStream.Position = value;
    }
    public override void Flush() => baseStream.Flush();
    public override int Read(byte[] buffer, int offset, int count)
        => baseStream.Read(buffer, offset, Math.Min(count, maxChunkSize));
    public override long Seek(long offset, SeekOrigin origin) => baseStream.Seek(offset, origin);
    public override void SetLength(long value) => baseStream.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => throw new System.NotImplementedException();
}
