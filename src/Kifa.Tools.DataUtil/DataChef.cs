using System;
using System.Collections.Generic;
using System.Linq;
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
                _ => null
            };
        }

        static string GetYamlType(string s) {
            return s == null || !s.StartsWith("#") ? null : s[1..s.IndexOf("\n", StringComparison.Ordinal)].Trim();
        }

        string ModelId { get; }
        KifaActionResult Import(string data);
        KifaActionResult<string> Export(string data, bool getAll);
        KifaActionResult Refresh(string id);
    }

    public class DataChef<TDataModel, TClient> : DataChef where TDataModel : DataModel
        where TClient : KifaServiceClient<TDataModel>, new() {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static KifaServiceClient<TDataModel> client;

        static KifaServiceClient<TDataModel> Client => client ??= new TClient();

        public string ModelId => Client.ModelId;

        public KifaActionResult Import(string data) {
            var items = new Deserializer().Deserialize<List<TDataModel>>(data);

            var results = new KifaBatchActionResult();
            foreach (var item in items) {
                results.Add(logger.LogResult(Client.Update(item), $"Update ({Client.ModelId}/{item.Id})"));
            }

            return results;
        }

        public KifaActionResult<string> Export(string data, bool getAll) {
            var items = new Deserializer().Deserialize<List<TDataModel>>(data).Select(item => item.Id).ToList();

            var updatedItems = getAll ? Client.List().Values.ToList() : Client.Get(items);

            var serializer = new SerializerBuilder().WithIndentedSequences()
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull).Build();
            return new KifaActionResult<string>(
                $"# {ModelId}\n{string.Join("\n", updatedItems.Select(item => serializer.Serialize(new List<TDataModel> {item})))}");
        }

        public KifaActionResult Refresh(string id) {
            return Client.Refresh(id);
        }
    }
}
