using System;
using System.Collections.Generic;

namespace Kifa.Bilibili.BilibiliApi;

// fnval=4048&
public class VideoUrlRpc {
    public class VideoUrlResponse {
        public long Code { get; set; }
        public long Message { get; set; }
        public long Ttl { get; set; }
        public Data Data { get; set; }
    }

    public class Data {
        public string From { get; set; }
        public string Result { get; set; }
        public string Message { get; set; }
        public long Quality { get; set; }
        public string Format { get; set; }
        public long Timelength { get; set; }
        public string AcceptFormat { get; set; }
        public List<string> AcceptDescription { get; set; }
        public List<long> AcceptQuality { get; set; }
        public long VideoCodecid { get; set; }
        public string SeekParam { get; set; }
        public string SeekType { get; set; }
        public Dash Dash { get; set; }
        public List<SupportFormat> SupportFormats { get; set; }
        public object HighFormat { get; set; }
    }

    public class Dash {
        public long Duration { get; set; }
        public double MinBufferTime { get; set; }
        public double DashMinBufferTime { get; set; }
        public List<Audio> Video { get; set; }
        public List<Audio> Audio { get; set; }
        public object Dolby { get; set; }
        public object Flac { get; set; }
    }

    public class Audio {
        public long Id { get; set; }
        public Uri BaseUrl { get; set; }
        public List<Uri> BackupUrl { get; set; }
        public long Bandwidth { get; set; }
        public string MimeType { get; set; }
        public string Codecs { get; set; }
        public long Width { get; set; }
        public long Height { get; set; }
        public string FrameRate { get; set; }
        public string Sar { get; set; }
        public long StartWithSap { get; set; }
        public SegmentBase SegmentBase { get; set; }
        public long Codecid { get; set; }
    }

    public class SegmentBase {
        public string Initialization { get; set; }
        public string IndexRange { get; set; }
    }

    public class SupportFormat {
        public long Quality { get; set; }
        public string Format { get; set; }
        public string NewDescription { get; set; }
        public string DisplayDesc { get; set; }
        public string Superscript { get; set; }
        public List<string> Codecs { get; set; }
    }
}
