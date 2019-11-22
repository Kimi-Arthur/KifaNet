using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using NLog;

namespace Pimix.Service {
    public class PimixServiceRestClient {
        internal static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static HttpClient client;

        internal static HttpClient Client
            => LazyInitializer.EnsureInitialized(ref client, ()
                => CertPath != null
                    ? new HttpClient(new HttpClientHandler {
                        ClientCertificates =
                            {new X509Certificate2(CertPath, CertPassword)}
                    })
                    : new HttpClient());

        public static string PimixServerApiAddress { get; set; }

        public static string CertPath { get; set; }
        public static string CertPassword { get; set; }
    }

    public class PimixServiceRestClient<TDataModel> : BasePimixServiceClient<TDataModel>
        where TDataModel : DataModel {
        const string IdDeliminator = "|";

        public override void Update(TDataModel data, string id = null) {
            id ??= data.Id;

            Retry.Run(() => {
                var request =
                    new HttpRequestMessage(new HttpMethod("PATCH"),
                        $"{PimixServiceRestClient.PimixServerApiAddress}/{modelId}/{Uri.EscapeDataString(id)}") {
                        Content = new StringContent(JsonConvert.SerializeObject(data, Defaults.JsonSerializerSettings),
                            Encoding.UTF8,
                            "application/json")
                    };

                using var response = PimixServiceRestClient.Client.SendAsync(request).Result;
                return response.GetObject<RestActionResult>().Status == RestActionStatus.OK;
            }, (ex, i) => HandleException(ex, i, $"Failure in PATCH {modelId}({id})"));
        }

        public override void Set(TDataModel data, string id = null) {
            id ??= data.Id;

            Retry.Run(() => {
                var request =
                    new HttpRequestMessage(HttpMethod.Post,
                        $"{PimixServiceRestClient.PimixServerApiAddress}/{modelId}/{Uri.EscapeDataString(id)}") {
                        Content = new StringContent(JsonConvert.SerializeObject(data, Defaults.JsonSerializerSettings),
                            Encoding.UTF8,
                            "application/json")
                    };

                using var response = PimixServiceRestClient.Client.SendAsync(request).Result;
                return response.GetObject<RestActionResult>().Status == RestActionStatus.OK;
            }, (ex, i) => HandleException(ex, i, $"Failure in POST {modelId}({id})"));
        }

        public override TDataModel Get(string id) {
            return Retry.Run(() => {
                var request =
                    new HttpRequestMessage(HttpMethod.Get,
                        $"{PimixServiceRestClient.PimixServerApiAddress}/{modelId}/{Uri.EscapeDataString(id)}");

                using var response = PimixServiceRestClient.Client.SendAsync(request).Result;
                return response.GetObject<TDataModel>();
            }, (ex, i) => HandleException(ex, i, $"Failure in GET {modelId}({id})"));
        }

        public override List<TDataModel> Get(List<string> ids) =>
            ids.Any()
                ? Retry.Run(() => {
                        var request =
                            new HttpRequestMessage(HttpMethod.Get,
                                $"{PimixServiceRestClient.PimixServerApiAddress}/{modelId}/{string.Join(IdDeliminator, ids.Select(Uri.EscapeDataString))}");

                        using var response = PimixServiceRestClient.Client.SendAsync(request).Result;
                        return response.GetObject<Dictionary<string, TDataModel>>().Values.ToList();
                    },
                    (ex, i) => HandleException(ex, i,
                        $"Failure in GET {modelId}({string.Join(", ", ids)})"))
                : new List<TDataModel>();

        public override void Link(string targetId, string linkId) {
            Retry.Run(() => {
                    var request =
                        new HttpRequestMessage(HttpMethod.Get,
                            $"{PimixServiceRestClient.PimixServerApiAddress}/{modelId}/" +
                            $"^+{Uri.EscapeDataString(targetId)}|{Uri.EscapeDataString(linkId)}");

                    using var response = PimixServiceRestClient.Client.SendAsync(request).Result;
                    return response.GetObject<RestActionResult>().Status == RestActionStatus.OK;
                },
                (ex, i) => HandleException(ex, i,
                    $"Failure in LINK {modelId}({linkId}) to {modelId}({targetId})"));
        }

        public override void Delete(string id) {
            Retry.Run(() => {
                var request =
                    new HttpRequestMessage(HttpMethod.Delete,
                        $"{PimixServiceRestClient.PimixServerApiAddress}/{modelId}/{Uri.EscapeDataString(id)}");

                using var response = PimixServiceRestClient.Client.SendAsync(request).Result;
                return response.GetObject<RestActionResult>().Status == RestActionStatus.OK;
            }, (ex, i) => HandleException(ex, i, $"Failure in DELETE {modelId}({id})"));
        }

        public void Call(string action,
            string id = null, Dictionary<string, object> parameters = null)
            => Call<object>(action, id, parameters);

        public TResponse Call<TResponse>(string action,
            string id = null, Dictionary<string, object> parameters = null) {
            return Retry.Run(() => {
                    var request =
                        new HttpRequestMessage(HttpMethod.Post,
                            $"{PimixServiceRestClient.PimixServerApiAddress}/{modelId}/${action}");

                    if (parameters != null) {
                        if (id != null) {
                            parameters["id"] = id;
                        }

                        request.Content = new StringContent(JsonConvert.SerializeObject(parameters,
                                Defaults.JsonSerializerSettings),
                            Encoding.UTF8,
                            "application/json");
                    }

                    using var response = PimixServiceRestClient.Client.SendAsync(request).Result;
                    var result = response.GetObject<RestActionResult<TResponse>>();
                    if (result.Status == RestActionStatus.OK) {
                        return result.Response;
                    }

                    throw new RestActionFailedException {
                        Result = result
                    };
                },
                (ex, i) => HandleException(ex, i,
                    $"Failure in CALL {modelId}({id}).{action}({id})"));
        }

        static void HandleException(Exception ex, int index, string message) {
            if (index >= 5 || ex is RestActionFailedException ||
                ex is HttpRequestException &&
                ex.InnerException is SocketException socketException &&
                socketException.Message == "Device not configured") {
                throw ex;
            }

            PimixServiceRestClient.logger.Warn(ex, $"{message} ({index})");
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
    }
}
