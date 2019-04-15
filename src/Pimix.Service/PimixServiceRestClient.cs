using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using NLog;

namespace Pimix.Service {
    public class PimixServiceRestClient : PimixServiceClient {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static string PimixServerApiAddress { get; set; }

        public static string PimixServerCredential { get; set; }

        static readonly HttpClient client = new HttpClient();

        public override void Update<TDataModel>(TDataModel data, string id = null) {
            var (idProperty, modelId) = GetModelInfo<TDataModel>();
            id = id ?? idProperty.GetValue(data) as string;

            Retry.Run(() => {
                var request =
                    new HttpRequestMessage(new HttpMethod("PATCH"),
                        $"{PimixServerApiAddress}/{modelId}/{Uri.EscapeDataString(id)}") {
                        Content = new StringContent(JsonConvert.SerializeObject(data, Defaults.JsonSerializerSettings),
                            Encoding.UTF8,
                            "application/json")
                    };

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Basic", PimixServerCredential);

                using (var response = client.SendAsync(request).Result) {
                    return response.GetObject<RestActionResult>().Status == RestActionStatus.OK;
                }
            }, (ex, i) => HandleException(ex, i, $"Failure in PATCH {modelId}({id})"));
        }

        public override void Set<TDataModel>(TDataModel data, string id = null) {
            var (idProperty, modelId) = GetModelInfo<TDataModel>();
            id = id ?? idProperty.GetValue(data) as string;

            Retry.Run(() => {
                var request =
                    new HttpRequestMessage(HttpMethod.Post,
                        $"{PimixServerApiAddress}/{modelId}/{Uri.EscapeDataString(id)}") {
                        Content = new StringContent(JsonConvert.SerializeObject(data, Defaults.JsonSerializerSettings),
                            Encoding.UTF8,
                            "application/json")
                    };

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Basic", PimixServerCredential);

                using (var response = client.SendAsync(request).Result) {
                    return response.GetObject<RestActionResult>().Status == RestActionStatus.OK;
                }
            }, (ex, i) => HandleException(ex, i, $"Failure in POST {modelId}({id})"));
        }

        public override TDataModel Get<TDataModel>(string id) {
            var modelId = GetModelInfo<TDataModel>().modelId;

            return Retry.Run(() => {
                var request =
                    new HttpRequestMessage(HttpMethod.Get,
                        $"{PimixServerApiAddress}/{modelId}/{Uri.EscapeDataString(id)}");

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Basic", PimixServerCredential);

                using (var response = client.SendAsync(request).Result) {
                    return response.GetObject<TDataModel>();
                }
            }, (ex, i) => HandleException(ex, i, $"Failure in GET {modelId}({id})"));
        }

        public override void Link<TDataModel>(string targetId, string linkId) {
            var modelId = GetModelInfo<TDataModel>().modelId;

            Retry.Run(() => {
                    var request =
                        new HttpRequestMessage(HttpMethod.Get,
                            $"{PimixServerApiAddress}/{modelId}/" +
                            $"^+{Uri.EscapeDataString(targetId)}|{Uri.EscapeDataString(linkId)}");

                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Basic", PimixServerCredential);

                    using (var response = client.SendAsync(request).Result) {
                        return response.GetObject<RestActionResult>().Status == RestActionStatus.OK;
                    }
                },
                (ex, i) => HandleException(ex, i,
                    $"Failure in LINK {modelId}({linkId}) to {modelId}({targetId})"));
        }

        public override void Delete<TDataModel>(string id) {
            var modelId = GetModelInfo<TDataModel>().modelId;

            Retry.Run(() => {
                var request =
                    new HttpRequestMessage(HttpMethod.Delete,
                        $"{PimixServerApiAddress}/{modelId}/{Uri.EscapeDataString(id)}");

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Basic", PimixServerCredential);

                using (var response = client.SendAsync(request).Result) {
                    return response.GetObject<RestActionResult>().Status == RestActionStatus.OK;
                }
            }, (ex, i) => HandleException(ex, i, $"Failure in DELETE {modelId}({id})"));
        }

        public override TResponse Call<TDataModel, TResponse>(string action,
            string id = null, Dictionary<string, object> parameters = null) {
            var modelId = GetModelInfo<TDataModel>().modelId;

            return Retry.Run(() => {
                    var request =
                        new HttpRequestMessage(HttpMethod.Post,
                            $"{PimixServerApiAddress}/{modelId}/${action}");

                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Basic", PimixServerCredential);

                    if (parameters != null) {
                        if (id != null) {
                            parameters["id"] = id;
                        }

                        request.Content = new StringContent(
                            JsonConvert.SerializeObject(parameters, Defaults.JsonSerializerSettings),
                            Encoding.UTF8,
                            "application/json");
                    }

                    using (var response = client.SendAsync(request).Result) {
                        var result = response.GetObject<RestActionResult<TResponse>>();
                        if (result.Status == RestActionStatus.OK) {
                            return result.Response;
                        }

                        throw new RestActionFailedException {Result = result};
                    }
                },
                (ex, i) => HandleException(ex, i,
                    $"Failure in CALL {modelId}({id}).{action}({id})"));
        }


        static void HandleException(Exception ex, int index, string message) {
            if (index >= 5 || ex is RestActionFailedException ||
                ex is HttpRequestException && ex.InnerException is SocketException socketException &&
                socketException.Message == "Device not configured") {
                throw ex;
            }

            logger.Warn(ex, $"{message} ({index})");
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
    }
}
