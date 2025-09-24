using System.Net.Http;
using System.Web;
using Kifa.Rpc;

namespace Kifa.YouTube;

public sealed class ArchiveItemDetailRpc : KifaJsonParameterizedRpc<ArchiveItemDetailRpc.Response> {
    #region ArchiveItemDetailRpc.Response

    public class Response {
        public string DisplayId { get; set; }
        public string[] Tags { get; set; }
        public long LikeCount { get; set; }
        public string FormatId { get; set; }
        public string UploaderId { get; set; }
        public long Duration { get; set; }
        public object StartTime { get; set; }
        public AutomaticCaptions AutomaticCaptions { get; set; }
        public long PlaylistIndex { get; set; }
        public string Uploader { get; set; }
        public string Extractor { get; set; }
        public double Fps { get; set; }
        public long Width { get; set; }
        public string Playlist { get; set; }
        public string Description { get; set; }
        public object Resolution { get; set; }
        public string UploadDate { get; set; }
        public AutomaticCaptions Subtitles { get; set; }
        public string ExtractorKey { get; set; }
        public long NEntries { get; set; }
        public object EndTime { get; set; }
        public string Vcodec { get; set; }
        public string Ext { get; set; }
        public long Abr { get; set; }
        public long Height { get; set; }
        public object Vbr { get; set; }
        public string PlaylistTitle { get; set; }
        public string[] Categories { get; set; }
        public string Fulltitle { get; set; }
        public string Thumbnail { get; set; }
        public Ffprobe Ffprobe { get; set; }
        public object StretchedRatio { get; set; }
        public string Acodec { get; set; }
        public string BaseFilename { get; set; }
        public long DislikeCount { get; set; }
        public string PlaylistId { get; set; }
        public string Id { get; set; }
        public double AverageRating { get; set; }
        public long AgeLimit { get; set; }
        public Thumbnail[] Thumbnails { get; set; }
        public string Format { get; set; }
        public string Title { get; set; }
        public object IsLive { get; set; }
        public string WebpageUrlBasename { get; set; }
        public string Annotations { get; set; }
        public long ViewCount { get; set; }
        public string WebpageUrl { get; set; }
    }

    public class AutomaticCaptions {
    }

    public class Ffprobe {
        public Stream[] Streams { get; set; }
        public ProgramVersion ProgramVersion { get; set; }
        public Format Format { get; set; }
        public object[] Chapters { get; set; }
    }

    public class Format {
        public FormatTags Tags { get; set; }
        public string StartTime { get; set; }
        public string Size { get; set; }
        public long ProbeScore { get; set; }
        public long NbStreams { get; set; }
        public long NbPrograms { get; set; }
        public string FormatName { get; set; }
        public string FormatLongName { get; set; }
        public string Duration { get; set; }
        public string BitRate { get; set; }
    }

    public class FormatTags {
        public string Encoder { get; set; }
    }

    public class ProgramVersion {
        public string Version { get; set; }
    }

    public class Stream {
        public long? Width { get; set; }
        public string TimeBase { get; set; }
        public StreamTags Tags { get; set; }
        public string StartTime { get; set; }
        public long StartPts { get; set; }
        public string SampleAspectRatio { get; set; }
        public long? Refs { get; set; }
        public string RFrameRate { get; set; }
        public string Profile { get; set; }
        public string PixFmt { get; set; }
        public long? Level { get; set; }
        public long Index { get; set; }
        public long? Height { get; set; }
        public long? HasBFrames { get; set; }
        public Disposition Disposition { get; set; }
        public string DisplayAspectRatio { get; set; }
        public string ColorRange { get; set; }
        public long? CodedWidth { get; set; }
        public long? CodedHeight { get; set; }
        public string CodecType { get; set; }
        public string CodecTimeBase { get; set; }
        public string CodecTagString { get; set; }
        public string CodecTag { get; set; }
        public string CodecName { get; set; }
        public string CodecLongName { get; set; }
        public string AvgFrameRate { get; set; }
        public string SampleRate { get; set; }
        public string SampleFmt { get; set; }
        public long? Channels { get; set; }
        public string ChannelLayout { get; set; }
        public long? BitsPerSample { get; set; }
    }

    public class Disposition {
        public long VisualImpaired { get; set; }
        public long TimedThumbnails { get; set; }
        public long Original { get; set; }
        public long Lyrics { get; set; }
        public long Karaoke { get; set; }
        public long HearingImpaired { get; set; }
        public long Forced { get; set; }
        public long Dub { get; set; }
        public long Default { get; set; }
        public long Comment { get; set; }
        public long CleanEffects { get; set; }
        public long AttachedPic { get; set; }
    }

    public class StreamTags {
        public string Language { get; set; }
    }

    public class Thumbnail {
        public string Url { get; set; }
        public string Id { get; set; }
    }

    #endregion

    protected override string Url => "https://{domain}{dir}/{file}";

    protected override HttpMethod Method => HttpMethod.Get;

    public ArchiveItemDetailRpc(string domain, string dir, string file) {
        Parameters = new() {
            { "domain", domain },
            { "dir", dir },
            { "file", file },
        };
    }
}
