using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Apps.MomentCounter;
using Kifa.Cloud.Swisscom;
using Kifa.Infos;
using Kifa.Languages.German;
using Kifa.Languages.German.Goethe;
using Kifa.Memrise;
using Kifa.Music;
using Kifa.Service;
using NLog;
using YamlDotNet.Serialization;

namespace Kifa.Tools.DataUtil;

public interface DataChef {
    public static DataChef GetChef(string modelId, string content = null) {
        return (modelId ?? GetYamlType(content)) switch {
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
            _ => null
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

public class DataChef<TDataModel> : DataChef where TDataModel : DataModel<TDataModel>, new() {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static KifaServiceClient<TDataModel> client;

    static KifaServiceClient<TDataModel> Client
        => client ??= new KifaServiceRestClient<TDataModel>();

    // TODO: Should not rely implementation detail. 
    public string ModelId => Client.ModelId;

    public List<TDataModel> Load(string data)
        => new Deserializer().Deserialize<List<TDataModel>>(data);

    public KifaActionResult Import(string data) {
        var items = Load(data);

        return Logger.LogResult(Client.Update(items),
            $"updating {ModelId}({string.Join(", ", items.Select(item => item.Id))})");
    }

    public string Save(List<TDataModel> items, bool compact) {
        var serializerBuilder = new SerializerBuilder().WithIndentedSequences()
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull);
        if (compact) {
            serializerBuilder =
                serializerBuilder.WithEventEmitter(next
                    => new FlowStyleScalarSequenceEmitter(next));
        }

        var serializer = serializerBuilder.Build();

        return
            $"# {ModelId}\n{string.Join("\n", items.Select(item => serializer.Serialize(new List<TDataModel> { item })))}";
    }

    public KifaActionResult<string> Export(string data, bool getAll, bool compact) {
        var items = new Deserializer().Deserialize<List<TDataModel>>(data).Select(item => item.Id)
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
