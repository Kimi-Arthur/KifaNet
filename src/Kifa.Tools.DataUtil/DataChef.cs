using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kifa.Api.Files;
using Kifa.Service;
using NLog;
using YamlDotNet.Serialization;

namespace Kifa.Tools.DataUtil {
    public class DataChef<TDataModel, TClient> where TDataModel : DataModel
        where TClient : KifaServiceClient<TDataModel>, new() {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static KifaServiceClient<TDataModel> client;

        static KifaServiceClient<TDataModel> Client => client ??= new TClient();

        public KifaActionResult Import(KifaFile dataFile) {
            using var reader = new StreamReader(dataFile.OpenRead());
            var items = new Deserializer().Deserialize<List<TDataModel>>(reader.ReadToEnd());

            var results = new KifaBatchActionResult();
            foreach (var item in items) {
                results.Add(logger.LogResult(Client.Update(item), $"Update ({Client.ModelId}/{item.Id})"));
            }

            return results;
        }

        public KifaActionResult Export(KifaFile dataFile) {
            using var reader = new StreamReader(dataFile.OpenRead());
            var items = new Deserializer().Deserialize<List<TDataModel>>(reader.ReadToEnd()).Select(item => item.Id)
                .ToList();

            var updatedItems = Client.Get(items);

            dataFile.Delete();

            var serializer = new SerializerBuilder().WithIndentedSequences()
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull).Build();
            dataFile.Write(string.Join("\n",
                updatedItems.Select(item => serializer.Serialize(new List<TDataModel> {item}))));

            return KifaActionResult.SuccessActionResult;
        }

        public KifaActionResult Refresh(string id) {
            return Client.Refresh(id);
        }
    }
}
