using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Apps.MomentCounter;
using Kifa.Cloud.Swisscom;
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
    public static DataChef GetChef(string modelId, string content = null) {
        return (modelId ?? GetYamlType(content)) switch {
            FileInformation.ModelId => new DataChef<FileInformation>(),
            MemriseCourse.ModelId => new DataChef<MemriseCourse>(),
            GoetheGermanWord.ModelId => new DataChef<GoetheGermanWord>(),
            GoetheWordList.ModelId => new DataChef<GoetheWordList>(),
            GermanWord.ModelId => new DataChef<GermanWord>(),
            GuitarChord.ModelId => new DataChef<GuitarChord>(),
            TvShow.ModelId => new DataChef<TvShow>(),
            Anime.ModelId => new DataChef<Anime>(),
            Unit.ModelId => new DataChef<Unit>(),
            User.ModelId => new DataChef<User>(),
            Event.ModelId => new DataChef<Event>(),
            Counter.ModelId => new DataChef<Counter>(),
            SwisscomAccount.ModelId => new DataChef<SwisscomAccount>(),
            SwisscomAccountQuota.ModelId => new DataChef<SwisscomAccountQuota>(),
            BiaoriJapaneseWord.ModelId => new DataChef<BiaoriJapaneseWord>(),
            _ => throw new Exception($"Invalid model id {modelId}")
        };
    }

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

public class DataChef<TDataModel> : DataChef where TDataModel : DataModel, new() {
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
