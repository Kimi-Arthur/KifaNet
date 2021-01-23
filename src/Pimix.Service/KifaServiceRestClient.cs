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
    public class KifaServiceRestClient {
        internal static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static HttpClient client;

        internal static HttpClient Client =>
            LazyInitializer.EnsureInitialized(ref client,
                () => ClientCertPath != null
                    ? new HttpClient(new HttpClientHandler {
                        ClientCertificates = {new X509Certificate2(ClientCertPath, ClientCertPassword)}
                    })
                    : new HttpClient());

        // Should probably be ending with `/api`.
        public static string ServerAddress { get; set; }
        
        // pfx cert path.
        public static string ClientCertPath { get; set; }
        
        // pfx cert password.
        public static string ClientCertPassword { get; set; }
    }

    public class KifaServiceRestClient<TDataModel> : BaseKifaServiceClient<TDataModel> where TDataModel : DataModel {
        const string IdDeliminator = "|";

        public override KifaActionResult Update(TDataModel data) =>
            KifaActionResult.FromAction(() => Retry.Run(() => {
                var request =
                    new HttpRequestMessage(new HttpMethod("PATCH"),
                        $"{KifaServiceRestClient.ServerAddress}/{modelId}/{Uri.EscapeDataString(data.Id)}") {
                        Content = new StringContent(JsonConvert.SerializeObject(data, Defaults.JsonSerializerSettings),
                            Encoding.UTF8, "application/json")
                    };

                using var response = KifaServiceRestClient.Client.SendAsync(request).Result;
                return response.GetObject<KifaActionResult>().Status == KifaActionStatus.OK;
            }, (ex, i) => HandleException(ex, i, $"Failure in PATCH {modelId}({data.Id})")));

        public override KifaActionResult Set(TDataModel data) =>
            KifaActionResult.FromAction(() => Retry.Run(() => {
                var request =
                    new HttpRequestMessage(HttpMethod.Post,
                        $"{KifaServiceRestClient.ServerAddress}/{modelId}/{Uri.EscapeDataString(data.Id)}") {
                        Content = new StringContent(JsonConvert.SerializeObject(data, Defaults.JsonSerializerSettings),
                            Encoding.UTF8, "application/json")
                    };

                using var response = KifaServiceRestClient.Client.SendAsync(request).Result;
                return response.GetObject<KifaActionResult>().Status == KifaActionStatus.OK;
            }, (ex, i) => HandleException(ex, i, $"Failure in POST {modelId}({data.Id})")));

        public override SortedDictionary<string, TDataModel> List() =>
            Retry.Run(() => {
                var request = new HttpRequestMessage(HttpMethod.Get,
                    $"{KifaServiceRestClient.ServerAddress}/{modelId}/");

                using var response = KifaServiceRestClient.Client.SendAsync(request).Result;
                return response.GetObject<SortedDictionary<string, TDataModel>>();
            }, (ex, i) => HandleException(ex, i, $"Failure in LIST {modelId}"));

        public override TDataModel Get(string id) =>
            Retry.Run(() => {
                var request = new HttpRequestMessage(HttpMethod.Get,
                    $"{KifaServiceRestClient.ServerAddress}/{modelId}/{Uri.EscapeDataString(id)}");

                using var response = KifaServiceRestClient.Client.SendAsync(request).Result;
                return response.GetObject<TDataModel>();
            }, (ex, i) => HandleException(ex, i, $"Failure in GET {modelId}({id})"));

        public override List<TDataModel> Get(List<string> ids) =>
            ids.Any()
                ? Retry.Run(() => {
                    var request = new HttpRequestMessage(HttpMethod.Get,
                        $"{KifaServiceRestClient.ServerAddress}/{modelId}/{string.Join(IdDeliminator, ids.Select(Uri.EscapeDataString))}");

                    using var response = KifaServiceRestClient.Client.SendAsync(request).Result;
                    return response.GetObject<Dictionary<string, TDataModel>>().Values.ToList();
                }, (ex, i) => HandleException(ex, i, $"Failure in GET {modelId}({string.Join(", ", ids)})"))
                : new List<TDataModel>();

        public override KifaActionResult Link(string targetId, string linkId) =>
            KifaActionResult.FromAction(() => Retry.Run(() => {
                var request = new HttpRequestMessage(HttpMethod.Get,
                    $"{KifaServiceRestClient.ServerAddress}/{modelId}/" +
                    $"^+{Uri.EscapeDataString(targetId)}|{Uri.EscapeDataString(linkId)}");

                using var response = KifaServiceRestClient.Client.SendAsync(request).Result;
                return response.GetObject<KifaActionResult>().Status == KifaActionStatus.OK;
            }, (ex, i) => HandleException(ex, i, $"Failure in LINK {modelId}({linkId}) to {modelId}({targetId})")));

        public override KifaActionResult Delete(string id) =>
            KifaActionResult.FromAction(() => Retry.Run(() => {
                var request = new HttpRequestMessage(HttpMethod.Delete,
                    $"{KifaServiceRestClient.ServerAddress}/{modelId}/{Uri.EscapeDataString(id)}");

                using var response = KifaServiceRestClient.Client.SendAsync(request).Result;
                return response.GetObject<KifaActionResult>().Status == KifaActionStatus.OK;
            }, (ex, i) => HandleException(ex, i, $"Failure in DELETE {modelId}({id})")));

        public void Call(string action, string id = null, Dictionary<string, object> parameters = null) =>
            Call<object>(action, id, parameters);

        public TResponse Call<TResponse>(string action, string id = null,
            Dictionary<string, object> parameters = null) {
            return Retry.Run(() => {
                var request = new HttpRequestMessage(HttpMethod.Post,
                    $"{KifaServiceRestClient.ServerAddress}/{modelId}/${action}");

                parameters ??= new Dictionary<string, object>();
                if (id != null) {
                    parameters["id"] = id;
                }

                request.Content =
                    new StringContent(JsonConvert.SerializeObject(parameters, Defaults.JsonSerializerSettings),
                        Encoding.UTF8, "application/json");

                using var response = KifaServiceRestClient.Client.SendAsync(request).Result;
                var result = response.GetObject<KifaActionResult<TResponse>>();
                if (result.Status == KifaActionStatus.OK) {
                    return result.Response;
                }

                throw new KifaActionFailedException {ActionResult = result};
            }, (ex, i) => HandleException(ex, i, $"Failure in CALL {modelId}({id}).{action}({id})"));
        }

        public override KifaActionResult Refresh(string id) => KifaActionResult.FromAction(() => Call("refresh", id));

        static void HandleException(Exception ex, int index, string message) {
            if (index >= 5 || ex is KifaActionFailedException || ex is HttpRequestException &&
                ex.InnerException is SocketException socketException &&
                socketException.Message == "Device not configured") {
                throw ex;
            }

            KifaServiceRestClient.logger.Warn(ex, $"{message} ({index})");
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
    }
}
