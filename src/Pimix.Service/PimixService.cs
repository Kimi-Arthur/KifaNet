using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using NLog;

namespace Pimix.Service {
    public static class PimixService {
        static readonly Dictionary<Type, Tuple<PropertyInfo, string>> typeCache
            = new Dictionary<Type, Tuple<PropertyInfo, string>>();

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static string PimixServerApiAddress { get; set; }

        public static string PimixServerCredential { get; set; }

        static readonly HttpClient client = new HttpClient();

        public static bool Patch<TDataModel>(TDataModel data, string id = null) {
            Init(typeof(TDataModel));
            var typeInfo = typeCache[typeof(TDataModel)];

            id = id ?? typeInfo.Item1.GetValue(data) as string;

            return Retry.Run(() => {
                var request =
                    new HttpRequestMessage(new HttpMethod("PATCH"),
                        $"{PimixServerApiAddress}/{typeInfo.Item2}/{Uri.EscapeDataString(id)}") {
                        Content = new StringContent(JsonConvert.SerializeObject(data),
                            Encoding.UTF8,
                            "application/json")
                    };

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Basic", PimixServerCredential);

                using (var response = client.SendAsync(request).Result) {
                    return response.GetObject<ActionResult>().StatusCode == ActionStatusCode.OK;
                }
            }, (ex, i) => HandleException(ex, i, $"Failure in PATCH {typeInfo.Item2}({id})"));
        }

        public static bool Post<TDataModel>(TDataModel data, string id = null) {
            Init(typeof(TDataModel));
            var typeInfo = typeCache[typeof(TDataModel)];

            id = id ?? typeInfo.Item1.GetValue(data) as string;

            return Retry.Run(() => {
                var request =
                    new HttpRequestMessage(HttpMethod.Post,
                        $"{PimixServerApiAddress}/{typeInfo.Item2}/{Uri.EscapeDataString(id)}") {
                        Content = new StringContent(JsonConvert.SerializeObject(data),
                            Encoding.UTF8,
                            "application/json")
                    };

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Basic", PimixServerCredential);

                using (var response = client.SendAsync(request).Result) {
                    return response.GetObject<ActionResult>().StatusCode == ActionStatusCode.OK;
                }
            }, (ex, i) => HandleException(ex, i, $"Failure in POST {typeInfo.Item2}({id})"));
        }

        public static TDataModel Get<TDataModel>(string id) {
            Init(typeof(TDataModel));
            var typeInfo = typeCache[typeof(TDataModel)];

            return Retry.Run(() => {
                var request =
                    new HttpRequestMessage(HttpMethod.Get,
                        $"{PimixServerApiAddress}/{typeInfo.Item2}/{Uri.EscapeDataString(id)}");

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Basic", PimixServerCredential);

                using (var response = client.SendAsync(request).Result) {
                    return response.GetObject<TDataModel>();
                }
            }, (ex, i) => HandleException(ex, i, $"Failure in GET {typeInfo.Item2}({id})"));
        }

        public static bool Link<TDataModel>(string targetId, string linkId) {
            Init(typeof(TDataModel));
            var typeInfo = typeCache[typeof(TDataModel)];

            return Retry.Run(() => {
                    var request =
                        new HttpRequestMessage(HttpMethod.Get,
                            $"{PimixServerApiAddress}/{typeInfo.Item2}/" +
                            $"^+{Uri.EscapeDataString(targetId)}|{Uri.EscapeDataString(linkId)}");

                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Basic", PimixServerCredential);

                    using (var response = client.SendAsync(request).Result) {
                        return response.GetObject<ActionResult>().StatusCode == ActionStatusCode.OK;
                    }
                },
                (ex, i) => HandleException(ex, i,
                    $"Failure in LINK {typeInfo.Item2}({linkId}) to {typeInfo.Item2}({targetId})"));
        }

        public static bool Delete<TDataModel>(string id) {
            Init(typeof(TDataModel));
            var typeInfo = typeCache[typeof(TDataModel)];

            return Retry.Run(() => {
                var request =
                    new HttpRequestMessage(HttpMethod.Delete,
                        $"{PimixServerApiAddress}/{typeInfo.Item2}/{Uri.EscapeDataString(id)}");

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Basic", PimixServerCredential);

                using (var response = client.SendAsync(request).Result) {
                    return response.GetObject<ActionResult>().StatusCode == ActionStatusCode.OK;
                }
            }, (ex, i) => HandleException(ex, i, $"Failure in DELETE {typeInfo.Item2}({id})"));
        }

        public static TResponse Call<TDataModel, TResponse>(string action,
            string id = null, Dictionary<string, object> parameters = null) {
            Init(typeof(TDataModel));
            var typeInfo = typeCache[typeof(TDataModel)];

            return Retry.Run(() => {
                    var request =
                        new HttpRequestMessage(HttpMethod.Post,
                            $"{PimixServerApiAddress}/{typeInfo.Item2}/${action}");

                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Basic", PimixServerCredential);

                    if (parameters != null) {
                        if (id != null) {
                            parameters["id"] = id;
                        }

                        request.Content = new StringContent(JsonConvert.SerializeObject(parameters),
                            Encoding.UTF8,
                            "application/json");
                    }

                    using (var response = client.SendAsync(request).Result) {
                        var result = response.GetObject<ActionResult<TResponse>>();
                        if (result.StatusCode == ActionStatusCode.OK) {
                            return result.Response;
                        }

                        throw new ActionFailedException {Result = result};
                    }
                },
                (ex, i) => HandleException(ex, i,
                    $"Failure in CALL {typeInfo.Item2}({id}).{action}({id})"));
        }

        public static void Call<TDataModel>(string action,
            string id = null, Dictionary<string, object> parameters = null)
            => Call<TDataModel, object>(action, id, parameters);

        static void HandleException(Exception ex, int index, string message) {
            if (ex is ActionFailedException) {
                throw ex;
            }

            logger.Warn(ex, $"{message} ({index})");
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }

        static void Init(Type typeInfo) {
            JsonConvert.DefaultSettings =
                () => new JsonSerializerSettings {
                    NullValueHandling = NullValueHandling.Ignore,
                    MetadataPropertyHandling = MetadataPropertyHandling.Ignore
                };

            if (!typeCache.ContainsKey(typeInfo)) {
                // TODO: Will throw. Can add custom exceptions.
                var idProp = typeInfo.GetProperty("Id");
                var dmAttr = typeInfo.GetCustomAttribute<DataModelAttribute>();
                typeCache[typeInfo] = Tuple.Create(idProp, dmAttr.ModelId);
            }
        }
    }
}
