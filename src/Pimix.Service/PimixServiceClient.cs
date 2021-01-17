using System;
using System.Collections.Generic;
using NLog;

namespace Pimix.Service {
    public interface PimixServiceClient<TDataModel> where TDataModel : DataModel {
        SortedDictionary<string, TDataModel> List();
        TDataModel Get(string id);
        List<TDataModel> Get(List<string> ids);

        TDataModel GetOr(string id, Func<string, TDataModel> defaultValue = null);

        RestActionResult Set(TDataModel data);
        RestActionResult Update(TDataModel data);
        RestActionResult Delete(string id);
        RestActionResult Link(string targetId, string linkId);
        RestActionResult Refresh(string id);
    }

    public abstract class BasePimixServiceClient<TDataModel> : PimixServiceClient<TDataModel>
        where TDataModel : DataModel {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly string modelId;

        protected BasePimixServiceClient() {
            var typeInfo = typeof(TDataModel);
            modelId = (string) typeInfo.GetField("ModelId").GetValue(null);
        }

        public abstract SortedDictionary<string, TDataModel> List();

        public abstract TDataModel Get(string id);

        public TDataModel GetOr(string id, Func<string, TDataModel> defaultValue = null) {
            try {
                return Get(id);
            } catch (Exception ex) {
                logger.Warn(ex, $"Failed to get value for {modelId}/{id}.");
                return defaultValue?.Invoke(id);
            }
        }

        public abstract List<TDataModel> Get(List<string> ids);
        public abstract RestActionResult Set(TDataModel data);
        public abstract RestActionResult Update(TDataModel data);
        public abstract RestActionResult Delete(string id);
        public abstract RestActionResult Link(string targetId, string linkId);
        public abstract RestActionResult Refresh(string id);
    }
}
