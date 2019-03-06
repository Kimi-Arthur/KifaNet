using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using NLog;

namespace Pimix.Service {
    public abstract class PimixServiceClient {
        static readonly Dictionary<Type, (PropertyInfo idProperty, string modelId)> modelCache
            = new Dictionary<Type, (PropertyInfo idProperty, string modelId)>();

        protected static (PropertyInfo idProperty, string modelId) GetModelInfo<TDataModel>() {
            var typeInfo = typeof(TDataModel);
            if (!modelCache.ContainsKey(typeInfo)) {
                var idProp = typeInfo.GetProperty("Id");
                var dmAttr = typeInfo.GetCustomAttribute<DataModelAttribute>();
                modelCache[typeInfo] = (idProperty: idProp, modelId: dmAttr.ModelId);
            }

            return modelCache[typeInfo];
        }

        public abstract TDataModel Get<TDataModel>(string id);
        public abstract void Create<TDataModel>(TDataModel data, string id = null);
        public abstract void Update<TDataModel>(TDataModel data, string id = null);
        public abstract void Delete<TDataModel>(string id);
        public abstract void Link<TDataModel>(string targetId, string linkId);

        public abstract TResponse Call<TDataModel, TResponse>(string action,
            string id = null, Dictionary<string, object> parameters = null);
    }

    public static class PimixService {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static PimixServiceClient client = new PimixServiceRestClient();

        public static TDataModel Get<TDataModel>(string id) => client.Get<TDataModel>(id);

        public static TDataModel GetOr<TDataModel>(string id, Func<string, TDataModel> defaultValue = null) {
            try {
                return Get<TDataModel>(id);
            } catch (Exception ex) {
                var value = defaultValue != null ? defaultValue(id) : default(TDataModel);
                logger.Warn(ex, "Cannot get a value for {0}, using default value: {1}.", id, value);
                return value;
            }
        }

        public static void Create<TDataModel>(TDataModel data, string id = null) => client.Create(data, id);

        public static void Update<TDataModel>(TDataModel data, string id = null) => client.Update(data, id);

        public static void Delete<TDataModel>(string id) => client.Delete<TDataModel>(id);

        public static void Link<TDataModel>(string targetId, string linkId) =>
            client.Link<TDataModel>(targetId, linkId);

        public static TResponse Call<TDataModel, TResponse>(string action,
            string id = null, Dictionary<string, object> parameters = null) =>
            client.Call<TDataModel, TResponse>(action, id, parameters);

        public static void Call<TDataModel>(string action,
            string id = null, Dictionary<string, object> parameters = null)
            => Call<TDataModel, object>(action, id, parameters);

        public static TDataModel Copy<TDataModel>(TDataModel data) =>
            JsonConvert.DeserializeObject<TDataModel>(JsonConvert.SerializeObject(data));
    }
}
