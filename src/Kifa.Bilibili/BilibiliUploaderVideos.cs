using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Service;
using NLog;

namespace Kifa.Bilibili;

public class BilibiliUploaderVideos : DataModel, WithModelId<BilibiliUploaderVideos> {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static string ModelId => "bilibili/uploader_videos";

    public static KifaServiceClient<BilibiliUploaderVideos> Client { get; set; } =
        new KifaServiceRestClient<BilibiliUploaderVideos>();

    public override TimeSpan? RefreshInterval => TimeSpan.FromDays(1);

    public List<string> Aids { get; set; } = [];
    public List<string> RemovedAids { get; set; } = [];

    public override void Fill(bool deep = false) {
        if (Aids.Count == 0 && RemovedAids.Count == 0) {
            var legacy = BilibiliUploader.Client.Get(Id.Checked());
            if (legacy != null) {
                Aids = [.. legacy.Aids];
                RemovedAids = [.. legacy.RemovedAids];
            }
        }

        var list = deep ? GetAllVideos() : MergeVideos(GetNewVideos(), Aids);
        var removed = RemovedAids.ToHashSet();
        removed.UnionWith(Aids);
        removed.ExceptWith(list);

        RemovedAids = removed.OrderBy(v => long.Parse(v[2..])).ToList();
        Aids = list;
        Aids.Reverse();
    }

    static List<string> MergeVideos(List<string> newVideos, List<string> oldVideos) {
        var newHashes = new HashSet<string>(newVideos);
        return newVideos.Concat(oldVideos.Reverse<string>()
            .Where(video => !newHashes.Contains(video))).ToList();
    }

    List<string> GetAllVideos() {
        var data = HttpClients.GetBilibiliClient().Call(new UploaderVideoRpc(Id.Checked()))?.Data;
        if (data == null) {
            throw new DataNotFoundException($"Cannot find videos uploaded by {Id}.");
        }

        var list = GetAids(data).ToList();

        while (data.HasMore) {
            Thread.Sleep(TimeSpan.FromSeconds(5));
            data = HttpClients.GetBilibiliClient().Call(new UploaderVideoRpc(Id.Checked(), data.Offset))
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
        var data = HttpClients.GetBilibiliClient().Call(new UploaderVideoRpc(Id.Checked()))?.Data;
        if (data == null) {
            throw new DataNotFoundException($"Cannot find videos uploaded by {Id}.");
        }

        var list = GetAids(data).ToList();
        if (Aids.Contains(list.Last())) {
            return list;
        }

        while (data.HasMore) {
            Thread.Sleep(TimeSpan.FromSeconds(5));
            data = HttpClients.GetBilibiliClient().Call(new UploaderVideoRpc(Id.Checked(), data.Offset))
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
