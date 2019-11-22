using System;
using System.Collections.Generic;

namespace Pimix.Service {
    public interface PimixServiceClient<TDataModel> where TDataModel : DataModel {
        TDataModel Get(string id);
        List<TDataModel> Get(List<string> ids);

        TDataModel GetOr(string id, Func<string, TDataModel> defaultValue = null);

        void Set(TDataModel data, string id = null);
        void Update(TDataModel data, string id = null);
        void Delete(string id);
        void Link(string targetId, string linkId);
    }

    public abstract class BasePimixServiceClient<TDataModel> : PimixServiceClient<TDataModel>
        where TDataModel : DataModel {
        protected readonly string modelId;

        protected BasePimixServiceClient() {
            var typeInfo = typeof(TDataModel);
            modelId = (string) typeInfo.GetField("ModelId").GetValue(null);
        }

        public abstract TDataModel Get(string id);

        public TDataModel GetOr(string id, Func<string, TDataModel> defaultValue = null) {
            try {
                return Get(id);
            } catch (Exception) {
                return defaultValue?.Invoke(id);
            }
        }

        public abstract List<TDataModel> Get(List<string> ids);
        public abstract void Set(TDataModel data, string id = null);
        public abstract void Update(TDataModel data, string id = null);
        public abstract void Delete(string id);
        public abstract void Link(string targetId, string linkId);
    }
}
