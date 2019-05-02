using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using NLog;

namespace Pimix.Service {
    public abstract class PimixServiceClient<TDataModel> {
        static readonly Dictionary<Type, (PropertyInfo idProperty, string modelId)> modelCache
            = new Dictionary<Type, (PropertyInfo idProperty, string modelId)>();

        protected static (PropertyInfo idProperty, string modelId) GetModelInfo<T>() {
            var typeInfo = typeof(T);
            if (!modelCache.ContainsKey(typeInfo)) {
                var idProp = typeInfo.GetProperty("Id");
                var dmAttr = typeInfo.GetCustomAttribute<DataModelAttribute>();
                modelCache[typeInfo] = (idProperty: idProp, modelId: dmAttr.ModelId);
            }

            return modelCache[typeInfo];
        }

        public abstract TDataModel Get(string id);
        public abstract void Set(TDataModel data, string id = null);
        public abstract void Update(TDataModel data, string id = null);
        public abstract void Delete(string id);
        public abstract void Link(string targetId, string linkId);

        public abstract TResponse Call<TResponse>(string action,
            string id = null, Dictionary<string, object> parameters = null);
    }

    public static class PimixService {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static Dictionary<Type, object> clients = new Dictionary<Type, object>();

        static PimixServiceClient<TDataModel> GetClient<TDataModel>() {
            var t = typeof(TDataModel);
            if (!clients.ContainsKey(t)) {
                clients[t] = new PimixServiceRestClient<TDataModel>();
            }

            return clients[t] as PimixServiceRestClient<TDataModel>;
        }

        public static TDataModel Get<TDataModel>(string id) => GetClient<TDataModel>().Get(id);

        public static TDataModel GetOr<TDataModel>(string id, Func<string, TDataModel> defaultValue = null) {
            try {
                return Get<TDataModel>(id);
            } catch (Exception ex) {
                var value = defaultValue != null ? defaultValue(id) : default(TDataModel);
                logger.Warn(ex, "Cannot get a value for {0}, using default value: {1}.", id, value);
                return value;
            }
        }

        public static void Create<TDataModel>(TDataModel data, string id = null) =>
            GetClient<TDataModel>().Set(data, id);

        public static void Update<TDataModel>(TDataModel data, string id = null) =>
            GetClient<TDataModel>().Update(data, id);

        public static void Delete<TDataModel>(string id) => GetClient<TDataModel>().Delete(id);

        public static void Link<TDataModel>(string targetId, string linkId) =>
            GetClient<TDataModel>().Link(targetId, linkId);

        public static TResponse Call<TDataModel, TResponse>(string action,
            string id = null, Dictionary<string, object> parameters = null) =>
            GetClient<TDataModel>().Call<TResponse>(action, id, parameters);

        public static void Call<TDataModel>(string action,
            string id = null, Dictionary<string, object> parameters = null)
            => Call<TDataModel, object>(action, id, parameters);

        public static TDataModel Copy<TDataModel>(TDataModel data) =>
            JsonConvert.DeserializeObject<TDataModel>(JsonConvert.SerializeObject(data));
    }
}
