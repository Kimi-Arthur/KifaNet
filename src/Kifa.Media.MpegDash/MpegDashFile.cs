using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Xml.Serialization;
using Kifa.IO;
using NLog;

namespace Kifa.Media.MpegDash;

public class MpegDashFile {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public string BaseUri { get; set; }

    public DashInfo DashInfo { get; set; }

    static readonly HttpClient HttpClient = new();

    public MpegDashFile(string manifestUri) {
        if (!manifestUri.EndsWith("/Manifest")) {
            throw new ArgumentException("Manifest uri should end with '/Manifest'",
                nameof(manifestUri));
        }

        BaseUri = manifestUri[..manifestUri.LastIndexOf("/Manifest")];
        var xml = new XmlSerializer(typeof(DashInfo));
        DashInfo = (DashInfo) xml.Deserialize(HttpClient.GetStreamAsync(manifestUri).Result)!;
    }

    public MpegDashFile(string baseUri, Stream stream) {
        BaseUri = baseUri;
        var xml = new XmlSerializer(typeof(DashInfo));
        DashInfo = (DashInfo) xml.Deserialize(stream)!;
    }

    public (Func<Stream> videoStreamGetter, List<Func<Stream>> audioStreamGetters) GetStreams() {
        var links = GetLinks();

        return (
            () => new ConcatenatedReadStream(links.VideoLinks.Select(GetStreamFromLink).ToList()),
            links.AudioLinks.Select<List<string>, Func<Stream>>(audioLinks => (()
                    => new ConcatenatedReadStream(audioLinks.Select(GetStreamFromLink).ToList())))
                .ToList());
    }

    static Stream GetStreamFromLink(string link)
        => new SeekableReadStream(() => HttpClient.GetContentLength(link)!.Value,
            (buffer, bufferOffset, offset, count) => {
                if (count < 0) {
                    count = buffer.Length - bufferOffset;
                }

                Logger.Trace($"Downloading from {offset} to {offset + count} of {link}...");

                return Retry.Run(() => {
                    var request = new HttpRequestMessage(HttpMethod.Get, link);

                    using var response = HttpClient.SendAsync(request).Result;
                    response.EnsureSuccessStatusCode();
                    var stream = response.Content.ReadAsStreamAsync().Result;
                    stream.Seek(offset, SeekOrigin.Begin);
                    stream.Read(buffer, bufferOffset, count);
                    return count;
                }, (ex, i) => {
                    if (i >= 5) {
                        throw ex;
                    }

                    Logger.Warn(ex, $"Download from {offset} to {offset + count} failed ({i})...");
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                });
            });

    public (List<string> VideoLinks, List<List<string>> AudioLinks) GetLinks() {
        List<string>? videoLinks = null;
        var audioLinks = new List<List<string>>();
        foreach (var set in DashInfo.Period.AdaptationSet) {
            if (set.ContentType == "video") {
                videoLinks = GetLinksFromAdaptationSet(set);
            }

            if (set.ContentType == "audio") {
                audioLinks.Add(GetLinksFromAdaptationSet(set));
            }
        }

        if (videoLinks == null || audioLinks.Count == 0) {
            var videoCount = videoLinks == null ? 0 : 1;
            throw new ResourceNotFoundException(
                $"Couldn't find video or audio links: video: {videoCount}, audio: {audioLinks.Count}");
        }

        return (videoLinks, audioLinks);
    }

    List<string> GetLinksFromAdaptationSet(AdaptationSet set) {
        var links = new List<string>();

        var bandwidth = set.Representation.Select(r => int.Parse(r.Bandwidth)).Max().ToString();

        var initTemplate = set.SegmentTemplate.Initialization;
        links.Add(BaseUri + "/" + initTemplate.Replace("$Bandwidth$", bandwidth));

        var mediaTemplate = set.SegmentTemplate.Media.Replace("$Bandwidth$", bandwidth);
        var time = 0L;
        foreach (var segment in set.SegmentTemplate.SegmentTimeline.S) {
            if (segment.T != null) {
                time = long.Parse(segment.T);
            }

            for (var i = 0; i < (segment.R == null ? 1 : int.Parse(segment.R) + 1); i++) {
                links.Add(BaseUri + "/" + mediaTemplate.Replace("$Time$", time.ToString()));
                time += long.Parse(segment.D);
            }
        }

        return links;
    }
}
