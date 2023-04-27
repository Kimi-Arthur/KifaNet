using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Bilibili.BilibiliApi;

public sealed class VideoUrlRpc : KifaJsonParameterizedRpc<VideoUrlResponse> {
    public override string UrlPattern
        => "https://api.bilibili.com/x/player/playurl?cid={cid}&avid={aid}&qn={quality}&fnval=4048&fourk=1";

    public override HttpMethod Method { get; } = HttpMethod.Get;

    public VideoUrlRpc(string aid, string cid, int quality) {
        parameters = new Dictionary<string, string> {
            { "aid", aid.Substring(2) },
            { "cid", cid },
            { "quality", quality.ToString() }
        };
    }
}

public class VideoUrlResponse {
    public long Code { get; set; }
    public string Message { get; set; }
    public long Ttl { get; set; }
    public Data? Data { get; set; }
}

public class Data {
    public string From { get; set; }
    public string Result { get; set; }
    public string Message { get; set; }
    public int Quality { get; set; }
    public string Format { get; set; }
    public long Timelength { get; set; }
    public string AcceptFormat { get; set; }
    public List<string> AcceptDescription { get; set; }
    public List<int> AcceptQuality { get; set; }
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
    public List<Resource>? Video { get; set; }
    public List<Resource>? Audio { get; set; }
    public Dash? Dolby { get; set; }
    public Dash? Flac { get; set; }
}

public class Resource {
    public int Id { get; set; }
    public string BaseUrl { get; set; }
    public List<string>? BackupUrl { get; set; }
    public long Bandwidth { get; set; }
    public string MimeType { get; set; }
    public string Codecs { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string FrameRate { get; set; }
    public string Sar { get; set; }
    public long StartWithSap { get; set; }
    public SegmentBase SegmentBase { get; set; }
    public int Codecid { get; set; }
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
