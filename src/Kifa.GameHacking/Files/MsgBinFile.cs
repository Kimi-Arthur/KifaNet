using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kifa.GameHacking.Files; 

public class MsgBinFile {
    public static IEnumerable<string> GetMessages(Stream data) {
        var reader = new BinaryReader(data);
        var indexes = new List<int> {
            reader.ReadInt32()
        };
        while (data.Position < indexes[0]) {
            indexes.Add(reader.ReadInt32());
        }

        indexes.Add((int) data.Length);

        for (int i = 0; i < indexes.Count - 1; i++) {
            var textBytes = new byte[indexes[i + 1] - indexes[i]];
            reader.Read(textBytes);
            yield return new string(Encoding.Unicode.GetString(textBytes)).Trim('\0');
        }
    }
}