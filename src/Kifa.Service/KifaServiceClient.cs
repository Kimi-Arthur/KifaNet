using System.Collections.Generic;
using NLog;

namespace Kifa.Service {
    public interface KifaServiceClient<TDataModel> where TDataModel : DataModel {
        string ModelId { get; }

        SortedDictionary<string, TDataModel> List();
        TDataModel Get(string id);
        List<TDataModel> Get(List<string> ids);
        KifaActionResult Set(TDataModel data);
        KifaActionResult Update(TDataModel data);
        KifaActionResult Delete(string id);
        KifaActionResult Link(string targetId, string linkId);
        KifaActionResult Refresh(string id);
    }

    public abstract class BaseKifaServiceClient<TDataModel> : KifaServiceClient<TDataModel>
        where TDataModel : DataModel {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        readonly string modelId;

        protected BaseKifaServiceClient() {
            var typeInfo = typeof(TDataModel);
            modelId = (string) typeInfo.GetField("ModelId").GetValue(null);
        }

        public string ModelId => modelId;

        public abstract SortedDictionary<string, TDataModel> List();
        public abstract TDataModel Get(string id);
        public abstract List<TDataModel> Get(List<string> ids);
        public abstract KifaActionResult Set(TDataModel data);
        public abstract KifaActionResult Update(TDataModel data);
        public abstract KifaActionResult Delete(string id);
        public abstract KifaActionResult Link(string targetId, string linkId);
        public abstract KifaActionResult Refresh(string id);
    }
}
