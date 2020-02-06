using System.IO;

namespace Pimix.Games.Files {
    public class LzssFile {
        const int BufferSize = 4096;

        public static Stream Decode(Stream encodedStream) {
            var rawStream = new MemoryStream();
            encodedStream.CopyTo(rawStream, 32 << 20);
            rawStream.Seek(0, SeekOrigin.Begin);
            var reader = new BinaryReader(rawStream);
            var dataSize = reader.ReadInt32();
            var buffer = new byte[BufferSize];
            var data = new byte[dataSize];
            var dataIndex = 0;
            var bufferWriteIndex = 0xFEE;
            while (rawStream.Position < rawStream.Length) {
                var flag = reader.ReadByte();
                for (int i = 0; i < 8; i++) {
                    if (rawStream.Position == rawStream.Length) {
                        break;
                    }

                    if ((flag & 1 << i) != 0) {
                        buffer[bufferWriteIndex++] = data[dataIndex++] = reader.ReadByte();
                        bufferWriteIndex %= BufferSize;
                    } else {
                        int bufferReadIndex = reader.ReadByte();
                        int b = reader.ReadByte();
                        bufferReadIndex |= (b & 0xF0) << 4;
                        for (int j = 0; j < (b & 0x0F) + 3; j++) {
                            buffer[bufferWriteIndex++] = data[dataIndex++] = buffer[bufferReadIndex++];
                            bufferReadIndex %= BufferSize;
                            bufferWriteIndex %= BufferSize;
                        }
                    }
                }
            }

            if (dataIndex != data.Length) {
                throw new DecodeException(
                    $"Decoded data doesn't have expected length ({dataIndex} instead of {data.Length}).");
            }

            return new MemoryStream(data);
        }
    }
}
