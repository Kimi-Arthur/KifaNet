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

        if (deep || Aids.Count == 0) {
            var allVideos = GetAllVideos();
            var removed = RemovedAids.ToHashSet();
            removed.UnionWith(Aids);
            removed.ExceptWith(allVideos);

            RemovedAids = removed.OrderBy(v => long.Parse(v[2..])).ToList();
            Aids = allVideos;
        } else {
            var newVideos = GetNewVideos();
            Aids.AddRange(newVideos);
        }
    }

    List<string> GetAllVideos() {
        var data = HttpClients.GetBilibiliClient().Call(new UploaderVideoRpc(Id.Checked()))?.Data;
        if (data == null) {
            throw new DataNotFoundException($"Cannot find videos uploaded by {Id}.");
        }

        var list = GetVideoItems(data).ToList();

        while (data.HasMore) {
            Thread.Sleep(TimeSpan.FromSeconds(5));
            data = HttpClients.GetBilibiliClient().Call(new UploaderVideoRpc(Id.Checked(), data.Offset))
                ?.Data;
            if (data == null) {
                throw new DataNotFoundException(
                    $"Cannot find videos uploaded by {Id} after {list.Count} videos.");
            }

            list.AddRange(GetVideoItems(data));
        }

        return list.DistinctBy(v => v.Aid)
            .OrderBy(v => v.PubTs)
            .ThenBy(v => long.Parse(v.Aid[2..]))
            .Select(v => v.Aid)
            .ToList();
    }

    // Gets new videos, sorted oldest to newest.
    List<string> GetNewVideos() {
        var data = HttpClients.GetBilibiliClient().Call(new UploaderVideoRpc(Id.Checked()))?.Data;
        if (data == null) {
            throw new DataNotFoundException($"Cannot find videos uploaded by {Id}.");
        }

        var existingSet = Aids.ToHashSet();
        var newVideos = new List<(string Aid, long PubTs, bool IsPinned)>();

        while (true) {
            var overlapFound = false;

            foreach (var item in GetVideoItems(data)) {
                if (existingSet.Contains(item.Aid)) {
                    if (item.IsPinned) {
                        continue;
                    }

                    overlapFound = true;
                    break;
                }

                newVideos.Add(item);
            }

            if (overlapFound || !data.HasMore) {
                break;
            }

            Thread.Sleep(TimeSpan.FromSeconds(5));
            data = HttpClients.GetBilibiliClient().Call(new UploaderVideoRpc(Id.Checked(), data.Offset))
                ?.Data;
            if (data == null) {
                throw new DataNotFoundException(
                    $"Cannot find videos uploaded by {Id} after {newVideos.Count} new videos.");
            }
        }

        return newVideos.DistinctBy(v => v.Aid)
            .OrderBy(v => v.PubTs)
            .ThenBy(v => long.Parse(v.Aid[2..]))
            .Select(v => v.Aid)
            .ToList();
    }

    static IEnumerable<(string Aid, long PubTs, bool IsPinned)> GetVideoItems(
        UploaderVideoRpc.Data data)
        => data.Items.Where(item => item.Modules.ModuleDynamic.Major?.Archive != null)
            .Select(item => (
                Aid: $"av{item.Modules.ModuleDynamic.Major?.Archive?.Aid}",
                PubTs: item.Modules.ModuleAuthor?.PubTs ?? 0,
                IsPinned: item.Modules.ModuleTag?.Text == "置顶"
            ));
}
