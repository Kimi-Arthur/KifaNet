namespace CG.Web.MegaApiClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    internal class MegaAesCtrStreamCrypter : MegaAesCtrStream
    {
        public MegaAesCtrStreamCrypter(Stream stream)
          : base(stream, stream.Length, Mode.Crypt, Crypto.CreateAesKey(), Crypto.CreateAesKey().CopySubArray(8))
        {
        }

        public byte[] FileKey
        {
            get { return this.fileKey; }
        }

        public byte[] Iv
        {
            get { return this.iv; }
        }

        public byte[] MetaMac { get; set; } = new byte[8];
    }

    internal class MegaAesCtrStreamDecrypter : MegaAesCtrStream
    {
        private readonly byte[] expectedMetaMac;

        public MegaAesCtrStreamDecrypter(Stream stream, long streamLength, byte[] fileKey, byte[] iv, byte[] expectedMetaMac)
          : base(stream, streamLength, Mode.Decrypt, fileKey, iv)
        {
            if (expectedMetaMac == null || expectedMetaMac.Length != 8)
            {
                throw new ArgumentException("Invalid expectedMetaMac");
            }

            this.expectedMetaMac = expectedMetaMac;
        }
    }

    internal abstract class MegaAesCtrStream : Stream
    {
        protected readonly byte[] fileKey;
        protected readonly byte[] iv;
        protected readonly long streamLength;
        protected byte[] metaMac = new byte[8];

        private readonly Stream stream;
        private readonly Mode mode;
        private readonly long[] chunksPositions;
        private readonly byte[] counter = new byte[8];
        private long currentCounter = 0;
        private byte[] currentChunkMac = new byte[16];
        private byte[] fileMac = new byte[16];

        protected MegaAesCtrStream(Stream stream, long streamLength, Mode mode, byte[] fileKey, byte[] iv)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (fileKey == null || fileKey.Length != 16)
            {
                throw new ArgumentException("Invalid fileKey");
            }

            if (iv == null || iv.Length != 8)
            {
                throw new ArgumentException("Invalid Iv");
            }

            this.stream = stream;
            this.streamLength = streamLength;
            this.mode = mode;
            this.fileKey = fileKey;
            this.iv = iv;

            this.chunksPositions = this.GetChunksPositions(this.streamLength);
        }

        protected enum Mode
        {
            Crypt,
            Decrypt
        }

        public long[] ChunksPositions
        {
            get { return this.chunksPositions; }
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

        protected virtual void OnStreamRead()
        {
        }

        private void IncrementCounter()
        {
            byte[] counter = BitConverter.GetBytes(this.currentCounter++);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(counter);
            }

            Array.Copy(counter, this.counter, 8);
        }

        private void ComputeChunk()
        {
            for (int i = 0; i < 16; i++)
            {
                this.fileMac[i] ^= this.currentChunkMac[i];
            }

            this.fileMac = Crypto.EncryptAes(this.fileMac, this.fileKey);
        }

        private long[] GetChunksPositions(long size)
        {
            List<long> chunks = new List<long>();
            chunks.Add(0);

            long chunkStartPosition = 0;
            for (int idx = 1; (idx <= 8) && (chunkStartPosition < (size - (idx * 131072))); idx++)
            {
                chunkStartPosition += idx * 131072;
                chunks.Add(chunkStartPosition);
            }

            while ((chunkStartPosition + 1048576) < size)
            {
                chunkStartPosition += 1048576;
                chunks.Add(chunkStartPosition);
            }

            return chunks.ToArray();
        }
    }
}
