using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Service;
using NLog;

namespace Kifa.Bilibili;

public class BilibiliUploader : DataModel, WithModelId<BilibiliUploader> {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static string ModelId => "bilibili/uploaders";

    public bool ForceFullRefresh { get; set; } = false;

    public static KifaServiceClient<BilibiliUploader> Client { get; set; } =
        new KifaServiceRestClient<BilibiliUploader>();

    public string Name { get; set; }
    public List<string> Aids { get; set; } = new();
    public List<string> RemovedAids { get; set; } = new();

    public override bool FillByDefault => true;

    public override DateTimeOffset? Fill() {
        var info = HttpClients.GetBilibiliClient().Call(new UploaderInfoWebRpc(Id));
        if (info == null) {
            throw new DataNotFoundException(
                $"Failed to retrieve data for uploader ({Id}) from bilibili,");
        }

        Name = info.Space.Info.Name;
        var list = ForceFullRefresh ? GetAllVideos() : MergeVideos(GetNewVideos(), Aids);
        var removed = RemovedAids.ToHashSet();
        removed.UnionWith(Aids);
        removed.ExceptWith(list);

        RemovedAids = removed.OrderBy(v => long.Parse(v[2..])).ToList();
        Aids = list;
        Aids.Reverse();

        return null;
    }

    static List<string> MergeVideos(List<string> newVideos, List<string> oldVideos) {
        var newHashes = new HashSet<string>(newVideos);
        return newVideos.Concat(oldVideos.Reverse<string>()
            .Where(video => !newHashes.Contains(video))).ToList();
    }

    List<string> GetAllVideos() {
        var data = HttpClients.GetBilibiliClient().Call(new UploaderVideoRpc(Id))?.Data;
        if (data == null) {
            throw new DataNotFoundException($"Cannot find videos uploaded by {Id}.");
        }

        var page = 1;
        var list = GetAids(data).ToList();

        while (data.HasMore) {
            Thread.Sleep(TimeSpan.FromSeconds(5));
            data = HttpClients.GetBilibiliClient().Call(new UploaderVideoRpc(Id, data.Offset))
                ?.Data;
            if (data == null) {
                throw new DataNotFoundException(
                    $"Cannot find videos uploaded by {Id} after {list.Count} videos.");
            }

            list.AddRange(GetAids(data));
        }

        return list;
    }

    // Gets new videos, may contain duplicates.
    List<string> GetNewVideos() {
        var data = HttpClients.GetBilibiliClient().Call(new UploaderVideoRpc(Id))?.Data;
        if (data == null) {
            throw new DataNotFoundException($"Cannot find videos uploaded by {Id}.");
        }

        var page = 1;
        var list = GetAids(data).ToList();
        if (Aids.Contains(list.Last())) {
            return list;
        }

        while (data.HasMore) {
            Thread.Sleep(TimeSpan.FromSeconds(5));
            data = HttpClients.GetBilibiliClient().Call(new UploaderVideoRpc(Id, data.Offset))
                ?.Data;
            if (data == null) {
                throw new DataNotFoundException(
                    $"Cannot find videos uploaded by {Id} after {list.Count} videos.");
            }

            list.AddRange(GetAids(data));
            if (Aids.Contains(list.Last())) {
                return list;
            }
        }

        return list;
    }

    static IEnumerable<string> GetAids(UploaderVideoRpc.Data data)
        => data.Items.Select(item => item.Modules.ModuleDynamic.Major?.Archive).ExceptNull()
            .Select(archive => $"av{archive.Aid}");
}
