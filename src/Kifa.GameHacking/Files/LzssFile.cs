using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kifa.GameHacking.Files;

public class LzssFile {
    const int BufferSize = 4096;

    public static IEnumerable<(string name, MemoryStream data)> GetFiles(Stream stream) {
        var decodedStream = Decode(stream);
        var reader = new BinaryReader(decodedStream);
        int fileCount = reader.ReadUInt16();
        reader.ReadUInt16();
        var fileInfos = new (string name, uint offset, int length)[fileCount];
        for (var i = 0; i < fileCount; i++) {
            decodedStream.Seek(0x40 * i + 4, SeekOrigin.Begin);
            var offset = reader.ReadUInt32();
            var length = reader.ReadInt32();
            var index = reader.ReadUInt32();
            if (i != index) {
                throw new DecodeException($"Subfile index mismatch ({i} instead of {index}).");
            }

            var nameChars = reader.ReadChars(0x30);
            var nameString = new string(nameChars);
            fileInfos[i] = (nameString.Substring(0, nameString.IndexOf('\0')), offset, length);
        }

        var data = new byte[fileCount][];
        for (var i = 0; i < fileCount; i++) {
            decodedStream.Seek(fileInfos[i].offset, SeekOrigin.Begin);
            data[i] = new byte[fileInfos[i].length];
            decodedStream.Read(data[i], 0, fileInfos[i].length);
        }

        return fileInfos.Select((f, index) => (f.name, new MemoryStream(data[index])));
    }

    public static MemoryStream Decode(Stream encodedStream) {
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
            for (var i = 0; i < 8; i++) {
                if (rawStream.Position == rawStream.Length) {
                    break;
                }

                if ((flag & (1 << i)) != 0) {
                    buffer[bufferWriteIndex++] = data[dataIndex++] = reader.ReadByte();
                    bufferWriteIndex %= BufferSize;
                } else {
                    int bufferReadIndex = reader.ReadByte();
                    int b = reader.ReadByte();
                    bufferReadIndex |= (b & 0xF0) << 4;
                    for (var j = 0; j < (b & 0x0F) + 3; j++) {
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
