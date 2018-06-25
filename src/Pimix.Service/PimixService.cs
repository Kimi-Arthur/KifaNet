using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Newtonsoft.Json;

namespace Pimix.Service {
    public static class PimixService {
        static readonly Dictionary<Type, Tuple<PropertyInfo, string>> typeCache
            = new Dictionary<Type, Tuple<PropertyInfo, string>>();

        public static string PimixServerApiAddress { get; set; }

        public static string PimixServerCredential { get; set; }

        static HttpClient client = new HttpClient();

        static RetryPolicy defaultRetryPolicy;

        public static RetryPolicy DefaultRetryPolicy {
            get {
                if (defaultRetryPolicy == null) {
                    defaultRetryPolicy =
                        new RetryPolicy<PimixServiceTransientErrorDetectionStrategy>(5,
                            TimeSpan.FromSeconds(10));
                    defaultRetryPolicy.Retrying += (sender, args) => {
                        Console.Error.WriteLine(
                            "Pimix service call failed once, wait 10 seconds now:");
                        Console.Error.WriteLine(args.LastException);
                    };
                }

                return defaultRetryPolicy;
            }
            set => defaultRetryPolicy = value;
        }

        public static bool Patch<TDataModel>(TDataModel data, string id = null,
            RetryPolicy retryPolicy = null)
            => (retryPolicy ?? DefaultRetryPolicy).ExecuteAction(() => PatchWithoutRetry(data, id));

        public static bool PatchWithoutRetry<TDataModel>(TDataModel data, string id = null) {
            Init(typeof(TDataModel));
            var typeInfo = typeCache[typeof(TDataModel)];

            id = id ?? typeInfo.Item1.GetValue(data) as string;
            var content = JsonConvert.SerializeObject(data);
            var address =
                $"{PimixServerApiAddress}/{typeInfo.Item2}/{Uri.EscapeDataString(id)}";

            var request = WebRequest.CreateHttp(address);
            request.Method = "PATCH";
            request.Headers["Authorization"] =
                $"Basic {PimixServerCredential}";

            using (var sw = new StreamWriter(request.GetRequestStream())) {
                sw.Write(content);
            }

            using (var response = request.GetResponse()) {
                return response.GetObject<ActionResult>().StatusCode == ActionStatusCode.OK;
            }
        }

        public static bool Post<TDataModel>(TDataModel data, string id = null,
            RetryPolicy retryPolicy = null)
            => (retryPolicy ?? DefaultRetryPolicy).ExecuteAction(() => PostWithoutRetry(data, id));

        public static bool PostWithoutRetry<TDataModel>(TDataModel data, string id = null) {
            Init(typeof(TDataModel));
            var typeInfo = typeCache[typeof(TDataModel)];

            id = id ?? typeInfo.Item1.GetValue(data) as string;
            var content = JsonConvert.SerializeObject(data);
            var address =
                $"{PimixServerApiAddress}/{typeInfo.Item2}/{Uri.EscapeDataString(id)}";

            var request = WebRequest.CreateHttp(address);
            request.Method = "POST";
            request.Headers["Authorization"] =
                $"Basic {PimixServerCredential}";

            using (var sw = new StreamWriter(request.GetRequestStream())) {
                sw.Write(content);
            }

            using (var response = request.GetResponse()) {
                return response.GetObject<ActionResult>().StatusCode == ActionStatusCode.OK;
            }
        }

        public static TDataModel Get<TDataModel>(string id, RetryPolicy retryPolicy = null)
            => (retryPolicy ?? DefaultRetryPolicy).ExecuteAction(()
                => GetWithoutRetry<TDataModel>(id));

        public static TDataModel GetWithoutRetry<TDataModel>(string id) {
            Init(typeof(TDataModel));
            var typeInfo = typeCache[typeof(TDataModel)];

            var address =
                $"{PimixServerApiAddress}/{typeInfo.Item2}/{Uri.EscapeDataString(id)}";

            var request = WebRequest.CreateHttp(address);
            request.Method = "GET";
            request.Headers["Authorization"] =
                $"Basic {PimixServerCredential}";

            using (var response = request.GetResponse()) {
                return response.GetObject<TDataModel>();
            }
        }

        public static bool Link<TDataModel>(string targetId, string linkId,
            RetryPolicy retryPolicy = null)
            => (retryPolicy ?? DefaultRetryPolicy).ExecuteAction(()
                => LinkWithoutRetry<TDataModel>(targetId, linkId));

        public static bool LinkWithoutRetry<TDataModel>(string targetId, string linkId) {
            Init(typeof(TDataModel));
            var typeInfo = typeCache[typeof(TDataModel)];

            var address =
                $"{PimixServerApiAddress}/{typeInfo.Item2}/^+{Uri.EscapeDataString(targetId)}|{Uri.EscapeDataString(linkId)}";

            var request = WebRequest.CreateHttp(address);
            request.Method = "GET";
            request.Headers["Authorization"] =
                $"Basic {PimixServerCredential}";

            using (var response = request.GetResponse()) {
                return response.GetObject<ActionResult>().StatusCode == ActionStatusCode.OK;
            }
        }

        public static bool Delete<TDataModel>(string id, RetryPolicy retryPolicy = null)
            => (retryPolicy ?? DefaultRetryPolicy).ExecuteAction(()
                => DeleteWithoutRetry<TDataModel>(id));

        public static bool DeleteWithoutRetry<TDataModel>(string id) {
            Init(typeof(TDataModel));
            var typeInfo = typeCache[typeof(TDataModel)];

            var address =
                $"{PimixServerApiAddress}/{typeInfo.Item2}/{Uri.EscapeDataString(id)}";

            var request = WebRequest.CreateHttp(address);
            request.Method = "DELETE";
            request.Headers["Authorization"] =
                $"Basic {PimixServerCredential}";

            using (var response = request.GetResponse()) {
                return response.GetObject<ActionResult>().StatusCode == ActionStatusCode.OK;
            }
        }

        public static TResponse Call<TDataModel, TResponse>(string action,
            string id = null, Dictionary<string, object> parameters = null,
            RetryPolicy retryPolicy = null)
            => (retryPolicy ?? DefaultRetryPolicy).ExecuteAction(()
                => CallWithoutRetry<TDataModel, TResponse>(action, id, parameters));

        static TResponse CallWithoutRetry<TDataModel, TResponse>(string action,
            string id = null, Dictionary<string, object> parameters = null) {
            Init(typeof(TDataModel));
            var typeInfo = typeCache[typeof(TDataModel)];

            var request =
                new HttpRequestMessage(HttpMethod.Post, $"{PimixServerApiAddress}/{typeInfo.Item2}/${action}");

            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", PimixServerCredential);

            if (parameters != null) {
                if (id != null) parameters["id"] = id;
                request.Content = new StringContent(JsonConvert.SerializeObject(parameters), Encoding.UTF8,
                    "application/json");
            }


            using (var response = client.SendAsync(request).Result) {
                var result = response.GetObject<ActionResult<TResponse>>();
                if (result.StatusCode == ActionStatusCode.OK)
                    return result.Response;
                throw new ActionFailedException {Result = result};
            }
        }

        public static void Call<TDataModel>(string action,
            string id = null, Dictionary<string, object> parameters = null,
            RetryPolicy retryPolicy = null)
            => (retryPolicy ?? DefaultRetryPolicy).ExecuteAction(()
                => CallWithoutRetry<TDataModel>(action, id, parameters));

        static void CallWithoutRetry<TDataModel>(string action,
            string id = null, Dictionary<string, object> parameters = null)
            => CallWithoutRetry<TDataModel, object>(action, id, parameters);

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
