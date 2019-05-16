using System;
using System.Collections.Generic;
using NLog;

namespace Pimix.Service {
    public interface PimixServiceClient<TDataModel> where TDataModel : DataModel {
        TDataModel Get(string id);
        List<TDataModel> Get(IEnumerable<string> ids);

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
            } catch (Exception ex) {
                return defaultValue?.Invoke(id);
            }
        }

        public abstract List<TDataModel> Get(IEnumerable<string> ids);
        public abstract void Set(TDataModel data, string id = null);
        public abstract void Update(TDataModel data, string id = null);
        public abstract void Delete(string id);
        public abstract void Link(string targetId, string linkId);
    }

    public static class PimixService {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static readonly Dictionary<Type, object> clients = new Dictionary<Type, object>();

        static PimixServiceClient<TDataModel> GetClient<TDataModel>() where TDataModel : DataModel {
            var t = typeof(TDataModel);
            if (!clients.ContainsKey(t)) {
                clients[t] = new PimixServiceRestClient<TDataModel>();
            }

            return clients[t] as PimixServiceRestClient<TDataModel>;
        }

        public static void Create<TDataModel>(TDataModel data, string id = null) where TDataModel : DataModel =>
            GetClient<TDataModel>().Set(data, id);

        public static void Update<TDataModel>(TDataModel data, string id = null) where TDataModel : DataModel =>
            GetClient<TDataModel>().Update(data, id);

        public static void Delete<TDataModel>(string id) where TDataModel : DataModel =>
            GetClient<TDataModel>().Delete(id);

        public static void Link<TDataModel>(string targetId, string linkId) where TDataModel : DataModel =>
            GetClient<TDataModel>().Link(targetId, linkId);
    }
}
