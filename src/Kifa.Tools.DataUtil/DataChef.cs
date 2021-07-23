using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Apps.MomentCounter;
using Kifa.Infos;
using Kifa.Languages.German.Goethe;
using Kifa.Music;
using Kifa.Service;
using NLog;
using YamlDotNet.Serialization;

namespace Kifa.Tools.DataUtil {
    public interface DataChef {
        public static DataChef GetChef(string modelId, string content = null) {
            return (modelId ?? GetYamlType(content)) switch {
                GoetheGermanWord.ModelId => new DataChef<GoetheGermanWord, GoetheGermanWordRestServiceClient>(),
                GoetheWordList.ModelId => new DataChef<GoetheWordList, GoetheWordListRestServiceClient>(),
                GuitarChord.ModelId => new DataChef<GuitarChord, GuitarChordRestServiceClient>(),
                TvShow.ModelId => new DataChef<TvShow, TvShowRestServiceClient>(),
                Anime.ModelId => new DataChef<Anime, AnimeRestServiceClient>(),
                Unit.ModelId => new DataChef<Unit, UnitRestServiceClient>(),
                _ => null
            };
        }

        static string GetYamlType(string s) {
            return s == null || !s.StartsWith("#") ? null : s[1..s.IndexOf("\n", StringComparison.Ordinal)].Trim();
        }

        string ModelId { get; }
        KifaActionResult Import(string data);
        KifaActionResult<string> Export(string data, bool getAll, bool compact);
        KifaActionResult Refresh(string id);
        KifaActionResult Link(string target, string link);
    }

    public class DataChef<TDataModel, TClient> : DataChef where TDataModel : DataModel<TDataModel>, new()
        where TClient : KifaServiceClient<TDataModel>, new() {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static KifaServiceClient<TDataModel> client;

        static KifaServiceClient<TDataModel> Client => client ??= new TClient();

        public string ModelId => Client.ModelId;

        public KifaActionResult Import(string data) {
            var items = new Deserializer().Deserialize<List<TDataModel>>(data);

            return logger.LogResult(Client.Update(items),
                $"Update {Client.ModelId}({string.Join(", ", items.Select(item => item.Id))})");
        }

        public KifaActionResult<string> Export(string data, bool getAll, bool compact) {
            var items = new Deserializer().Deserialize<List<TDataModel>>(data).Select(item => item.Id).ToList();

            var updatedItems = getAll ? GetItemsWithExistingOrder(items, Client.List()) : Client.Get(items);

            var serializerBuilder = new SerializerBuilder().WithIndentedSequences()
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull);
            if (compact) {
                serializerBuilder =
                    serializerBuilder.WithEventEmitter(next => new FlowStyleScalarSequenceEmitter(next));
            }

            return new KifaActionResult<string>(
                $"# {ModelId}\n{string.Join("\n", updatedItems.Select(item => serializerBuilder.Build().Serialize(new List<TDataModel> {item})))}");
        }

        public KifaActionResult Refresh(string id) => Client.Refresh(id);

        public KifaActionResult Link(string target, string link) => Client.Link(target, link);

        static List<TDataModel> GetItemsWithExistingOrder(IEnumerable<string> items,
            SortedDictionary<string, TDataModel> list) =>
            items.Select(list.Pop).Concat(list.Values).ToList();
    }
}
