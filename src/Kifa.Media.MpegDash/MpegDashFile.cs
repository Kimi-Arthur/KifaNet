using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Kifa.Media.MpegDash;

public class MpegDashFile {
    public DashInfo DashInfo { get; set; }

    public MpegDashFile(Stream stream) {
        var xml = new XmlSerializer(typeof(DashInfo));
        DashInfo = (DashInfo) xml.Deserialize(stream)!;
    }

    public (string extension, int quality, Func<Stream> videoStreamGetter, List<Func<Stream>>
        audioStreamGetters) GetStreams() {
        return ("", 1, () => new MemoryStream(), new List<Func<Stream>>());
    }
}
