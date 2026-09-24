using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Apps.MomentCounter;
using Kifa.Bilibili;
using Kifa.Cloud.Swisscom;
using Kifa.Cloud.Telegram;
using Kifa.Infos;
using Kifa.IO;
using Kifa.Languages.Biaori;
using Kifa.Languages.German;
using Kifa.Languages.Goethe;
using Kifa.Languages.Memrise;
using Kifa.Music;
using Kifa.Service;
using Kifa.YouTube;
using NLog;
using YamlDotNet.Serialization;

namespace Kifa.Tools.DataUtil;

public interface DataChef {
    static readonly Dictionary<string, Lazy<DataChef>> Chefs = new() {
        { FileInformation.ModelId, new Lazy<DataChef>(() => new DataChef<FileInformation>()) },
        { MemriseCourse.ModelId, new Lazy<DataChef>(() => new DataChef<MemriseCourse>()) },
        { GoetheGermanWord.ModelId, new Lazy<DataChef>(() => new DataChef<GoetheGermanWord>()) },
        { GoetheWordList.ModelId, new Lazy<DataChef>(() => new DataChef<GoetheWordList>()) },
        { GermanWord.ModelId, new Lazy<DataChef>(() => new DataChef<GermanWord>()) },
        { GuitarChord.ModelId, new Lazy<DataChef>(() => new DataChef<GuitarChord>()) },
        { TvShow.ModelId, new Lazy<DataChef>(() => new DataChef<TvShow>()) },
        { Anime.ModelId, new Lazy<DataChef>(() => new DataChef<Anime>()) },
        { Series.ModelId, new Lazy<DataChef>(() => new DataChef<Series>()) },
        { Unit.ModelId, new Lazy<DataChef>(() => new DataChef<Unit>()) },
        { User.ModelId, new Lazy<DataChef>(() => new DataChef<User>()) },
        { Event.ModelId, new Lazy<DataChef>(() => new DataChef<Event>()) },
        { Counter.ModelId, new Lazy<DataChef>(() => new DataChef<Counter>()) }, {
            BilibiliMangaEpisode.ModelId,
            new Lazy<DataChef>(() => new DataChef<BilibiliMangaEpisode>())
        },
        { SwisscomAccount.ModelId, new Lazy<DataChef>(() => new DataChef<SwisscomAccount>()) }, {
            SwisscomAccountQuota.ModelId,
            new Lazy<DataChef>(() => new DataChef<SwisscomAccountQuota>())
        },
        { TelegramAccount.ModelId, new Lazy<DataChef>(() => new DataChef<TelegramAccount>()) }, {
            TelegramStorageCell.ModelId,
            new Lazy<DataChef>(() => new DataChef<TelegramStorageCell>())
        },
        { BiaoriJapaneseWord.ModelId, new Lazy<DataChef>(() => new DataChef<BiaoriJapaneseWord>()) },
        { BilibiliUploader.ModelId, new Lazy<DataChef>(() => new DataChef<BilibiliUploader>()) }, {
            BilibiliUploaderVideos.ModelId,
            new Lazy<DataChef>(() => new DataChef<BilibiliUploaderVideos>())
        },
        { BilibiliVideo.ModelId, new Lazy<DataChef>(() => new DataChef<BilibiliVideo>()) },
        { BilibiliPlaylist.ModelId, new Lazy<DataChef>(() => new DataChef<BilibiliPlaylist>()) },
        { BilibiliManga.ModelId, new Lazy<DataChef>(() => new DataChef<BilibiliManga>()) },
        { BilibiliArchive.ModelId, new Lazy<DataChef>(() => new DataChef<BilibiliArchive>()) },
        { BilibiliBangumi.ModelId, new Lazy<DataChef>(() => new DataChef<BilibiliBangumi>()) },
        { YouTubeUploader.ModelId, new Lazy<DataChef>(() => new DataChef<YouTubeUploader>()) }, {
            YouTubeUploaderVideos.ModelId,
            new Lazy<DataChef>(() => new DataChef<YouTubeUploaderVideos>())
        },
        { YouTubeVideo.ModelId, new Lazy<DataChef>(() => new DataChef<YouTubeVideo>()) },
        { YouTubePlaylist.ModelId, new Lazy<DataChef>(() => new DataChef<YouTubePlaylist>()) }
    };

    public static DataChef? GetChef(string? modelId, string? content = null) {
        var key = modelId ?? (content != null ? GetYamlType(content) : null);
        return key != null && Chefs.TryGetValue(key, out var chef) ? chef.Value : null;
    }

    static string? GetYamlType(string s) {
        if (!s.StartsWith('#')) {
            return null;
        }

        var newlineIndex = s.IndexOf('\n');
        return newlineIndex < 0 ? s[1..].Trim() : s[1..newlineIndex].Trim();
    }

    string ModelId { get; }
    KifaActionResult Import(string data);
    KifaActionResult<string> Export(string data, bool getAll, bool compact);
    KifaActionResult Link(string target, string link);
    KifaActionResult Delete(List<string> ids);
    KifaActionResult Call(string action, string? data = null);
}

public class DataChef<TDataModel> : DataChef
    where TDataModel : DataModel, WithModelId<TDataModel>, new() {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static KifaServiceClient<TDataModel>? client;

    public static KifaServiceClient<TDataModel> Client {
        get => client ??= new KifaServiceRestClient<TDataModel>();
        set => client = value;
    }

    // TODO: Should not rely on implementation detail. 
    public string ModelId => Client.ModelId;

    static readonly IDeserializer Deserializer =
        new DeserializerBuilder().IgnoreUnmatchedProperties().Build();

    static readonly ISerializer CompactSerializer = new SerializerBuilder().WithIndentedSequences()
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull).WithEventEmitter(next
            => new FlowStyleScalarSequenceEmitter(next)).Build();

    static readonly ISerializer Serializer = new SerializerBuilder().WithIndentedSequences()
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull |
                                        DefaultValuesHandling.OmitEmptyCollections).Build();

    public List<TDataModel> Load(string data)
        => Deserializer.Deserialize<List<TDataModel>>(data) ?? [];

    public KifaActionResult Import(string data) {
        var items = Load(data);

        return Logger.LogResult(Client.Update(items),
            $"updating {ModelId}({string.Join(", ", items.Select(item => item.Id))})");
    }

    public string Save(List<TDataModel> items, bool compact) {
        var serializer = compact ? CompactSerializer : Serializer;

        return
            $"# {ModelId}\n{string.Join("\n", items.Select(item => serializer.Serialize(new List<TDataModel> { item })))}";
    }

    public KifaActionResult<string> Export(string data, bool getAll, bool compact) {
        var items = Load(data).Select(item => item.Id).ToList();

        var updatedItems =
            getAll ? GetItemsWithExistingOrder(items, Client.List()) : Client.Get(items);


        return new KifaActionResult<string>(Save(updatedItems, compact));
    }

    public KifaActionResult Link(string target, string link) => Client.Link(target, link);
    public KifaActionResult Delete(List<string> ids) => Client.Delete(ids);

    public KifaActionResult Call(string action, string? data = null) {
        var param = string.IsNullOrWhiteSpace(data) ? null : Deserializer.Deserialize<object>(data);
        if (Client is KifaRpcClient rpcClient) {
            return rpcClient.Call(action, param);
        }

        return new KifaActionResult {
            Status = KifaActionStatus.BadRequest,
            Message = $"Client for {ModelId} does not support RPC call."
        };
    }

    static List<TDataModel> GetItemsWithExistingOrder(IEnumerable<string> items,
        SortedDictionary<string, TDataModel> list)
        => items.Select(list.Pop).Concat(list.Values).ToList();
}
