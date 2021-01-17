using System;
using System.Collections.Generic;
using NLog;

namespace Pimix.Service {
    public interface PimixServiceClient<TDataModel> where TDataModel : DataModel {
        SortedDictionary<string, TDataModel> List();
        TDataModel Get(string id);
        List<TDataModel> Get(List<string> ids);

        TDataModel GetOr(string id, Func<string, TDataModel> defaultValue = null);

        void Set(TDataModel data);
        void Update(TDataModel data);
        void Delete(string id);
        void Link(string targetId, string linkId);
        void Refresh(string id);
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
        public abstract void Set(TDataModel data);
        public abstract void Update(TDataModel data);
        public abstract void Delete(string id);
        public abstract void Link(string targetId, string linkId);
        public abstract void Refresh(string id);
    }
}
