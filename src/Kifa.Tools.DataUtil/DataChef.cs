using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Apps.MomentCounter;
using Kifa.Bilibili;
using Kifa.Cloud.Swisscom;
using Kifa.Cloud.Telegram;
using Kifa.Infos;
using Kifa.IO;
using Kifa.Languages.German;
using Kifa.Languages.German.Goethe;
using Kifa.Languages.Japanese;
using Kifa.Memrise;
using Kifa.Music;
using Kifa.Service;
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
        { BiaoriJapaneseWord.ModelId, new Lazy<DataChef>(() => new DataChef<BiaoriJapaneseWord>()) }
    };

    public static DataChef GetChef(string? modelId, string? content = null)
        => Chefs[modelId ?? GetYamlType(content!)].Value;

    static string GetYamlType(string s)
        => s == null || !s.StartsWith("#")
            ? null
            : s[1..s.IndexOf("\n", StringComparison.Ordinal)].Trim();

    string ModelId { get; }
    KifaActionResult Import(string data);
    KifaActionResult<string> Export(string data, bool getAll, bool compact);
    KifaActionResult Link(string target, string link);
    KifaActionResult Delete(List<string> ids);
}

public class DataChef<TDataModel> : DataChef where TDataModel : DataModel, WithModelId<TDataModel>, new() {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static KifaServiceClient<TDataModel> client;

    static KifaServiceClient<TDataModel> Client
        => client ??= new KifaServiceRestClient<TDataModel>();

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

    public List<TDataModel> Load(string data) => Deserializer.Deserialize<List<TDataModel>>(data);

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
        var items = Deserializer.Deserialize<List<TDataModel>>(data).Select(item => item.Id)
            .ToList();

        var updatedItems =
            getAll ? GetItemsWithExistingOrder(items, Client.List()) : Client.Get(items);


        return new KifaActionResult<string>(Save(updatedItems, compact));
    }

    public KifaActionResult Link(string target, string link) => Client.Link(target, link);
    public KifaActionResult Delete(List<string> ids) => Client.Delete(ids);

    static List<TDataModel> GetItemsWithExistingOrder(IEnumerable<string> items,
        SortedDictionary<string, TDataModel> list)
        => items.Select(list.Pop).Concat(list.Values).ToList();
}
