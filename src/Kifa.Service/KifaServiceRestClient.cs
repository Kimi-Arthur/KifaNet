using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using NLog;

namespace Kifa.Service;

public class KifaServiceRestClient {
    internal static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static HttpClient? client;

    internal static HttpClient Client
        => client ??= ClientCertPath != null
            ? new HttpClient(new HttpClientHandler {
                ClientCertificates = {
                    new X509Certificate2(ClientCertPath, ClientCertPassword)
                }
            }) {
                Timeout = TimeSpan.FromMinutes(10)
            }
            : new HttpClient();

    // Should probably be ending with `/api`.
    public static string ServerAddress { get; set; } = "http://www.kifa.ga/api";

    // pfx cert path.
    public static string? ClientCertPath { get; set; }

    // pfx cert password.
    public static string? ClientCertPassword { get; set; }
}

public class KifaServiceRestClient<TDataModel> : BaseKifaServiceClient<TDataModel>
    where TDataModel : DataModel, WithModelId<TDataModel> {
    public override KifaActionResult Update(TDataModel data)
        => KifaActionResult.FromAction(() => Retry.Run(() => {
            var request = new HttpRequestMessage(new HttpMethod("PATCH"),
                GetUrl(Uri.EscapeDataString(data.Id.Checked()))) {
                Content = new StringContent(
                    JsonConvert.SerializeObject(data, KifaJsonSerializerSettings.Default),
                    Encoding.UTF8, "application/json")
            };

            return KifaServiceRestClient.Client.GetObject<KifaActionResult>(request) ??
                   KifaActionResult.UnknownError;
        }, (ex, i) => HandleException(ex, i, $"Failure in PATCH {ModelId}({data.Id})")));

    public override KifaActionResult Update(List<TDataModel> data)
        => KifaActionResult.FromAction(() => Retry.Run(() => {
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), GetUrl("$")) {
                    Content = new StringContent(
                        JsonConvert.SerializeObject(data, KifaJsonSerializerSettings.Default),
                        Encoding.UTF8, "application/json")
                };

                return KifaServiceRestClient.Client.GetObject<KifaActionResult>(request) ??
                       KifaActionResult.UnknownError;
            },
            (ex, i) => HandleException(ex, i,
                $"Failure in PATCH {ModelId}({string.Join(", ", data.Select(item => item.Id))})")));


    public override KifaActionResult Set(TDataModel data)
        => KifaActionResult.FromAction(() => Retry.Run(() => {
            var request = new HttpRequestMessage(HttpMethod.Post,
                GetUrl(Uri.EscapeDataString(data.Id.Checked()))) {
                Content = new StringContent(
                    JsonConvert.SerializeObject(data, KifaJsonSerializerSettings.Default),
                    Encoding.UTF8, "application/json")
            };

            return KifaServiceRestClient.Client.GetObject<KifaActionResult>(request) ??
                   KifaActionResult.UnknownError;
        }, (ex, i) => HandleException(ex, i, $"Failure in POST {ModelId}({data.Id})")));

    public override KifaActionResult Set(List<TDataModel> data)
        => KifaActionResult.FromAction(() => Retry.Run(() => {
                var request = new HttpRequestMessage(HttpMethod.Post, GetUrl("$")) {
                    Content = new StringContent(
                        JsonConvert.SerializeObject(data, KifaJsonSerializerSettings.Default),
                        Encoding.UTF8, "application/json")
                };

                return KifaServiceRestClient.Client.GetObject<KifaActionResult>(request) ??
                       KifaActionResult.UnknownError;
                ;
            },
            (ex, i) => HandleException(ex, i,
                $"Failure in POST {ModelId}({string.Join(", ", data.Select(item => item.Id))})")));

    public override SortedDictionary<string, TDataModel> List(string folder = "",
        bool recursive = true, KifaDataOptions? options = null)
        => Retry.Run(() => {
            var request = new HttpRequestMessage(HttpMethod.Get,
                GetUrl("", [$"recursive={recursive}", $"folder={folder}"], options));

            var result = KifaServiceRestClient.Client
                             .GetObject<SortedDictionary<string, TDataModel>>(request) ??
                         new SortedDictionary<string, TDataModel>();
            foreach (var kv in result) {
                kv.Value.Id = kv.Key;
            }

            return result;
        }, (ex, i) => HandleException(ex, i, $"Failure in LIST {ModelId}"));

    public override TDataModel? Get(string id, bool refresh = false,
        KifaDataOptions? options = null)
        => Retry.Run(() => {
            var request = new HttpRequestMessage(HttpMethod.Get,
                GetUrl(Uri.EscapeDataString(id), [$"refresh={refresh}"], options));

            return KifaServiceRestClient.Client.GetObject<TDataModel>(request);
        }, (ex, i) => HandleException(ex, i, $"Failure in GET {ModelId}({id})"));

    public override List<TDataModel?> Get(List<string> ids, KifaDataOptions? options = null)
        => ids.Count != 0
            ? Retry.Run(() => {
                    var request =
                        new HttpRequestMessage(HttpMethod.Get, GetUrl("$", options: options)) {
                            // Not supported by HTTP spec.
                            Content = new StringContent(
                                JsonConvert.SerializeObject(ids,
                                    KifaJsonSerializerSettings.Default),
                                Encoding.UTF8, "application/json")
                        };

                    return KifaServiceRestClient.Client.GetObject<List<TDataModel?>>(request)!;
                },
                (ex, i) => HandleException(ex, i,
                    $"Failure in GET {ModelId}({string.Join(", ", ids)})"))
            : [];

    public override KifaActionResult Link(string targetId, string linkId)
        => KifaActionResult.FromAction(() => Retry.Run(() => {
                var request = new HttpRequestMessage(HttpMethod.Post, GetUrl("^")) {
                    Content = new StringContent(JsonConvert.SerializeObject(new List<string> {
                        targetId,
                        linkId
                    }, KifaJsonSerializerSettings.Default), Encoding.UTF8, "application/json")
                };

                return KifaServiceRestClient.Client.GetObject<KifaActionResult>(request) ??
                       KifaActionResult.UnknownError;
            },
            (ex, i) => HandleException(ex, i,
                $"Failure in LINK {ModelId}({linkId}) to {ModelId}({targetId})")));

    public override KifaActionResult Delete(string id)
        => KifaActionResult.FromAction(() => Retry.Run(() => {
            var request =
                new HttpRequestMessage(HttpMethod.Delete, GetUrl(Uri.EscapeDataString(id)));

            return KifaServiceRestClient.Client.GetObject<KifaActionResult>(request) ??
                   KifaActionResult.UnknownError;
        }, (ex, i) => HandleException(ex, i, $"Failure in DELETE {ModelId}({id})")));

    public override KifaActionResult Delete(List<string> ids)
        => KifaActionResult.FromAction(() => Retry.Run(() => {
                var request = new HttpRequestMessage(HttpMethod.Delete, GetUrl("$")) {
                    Content = new StringContent(
                        JsonConvert.SerializeObject(ids, KifaJsonSerializerSettings.Default),
                        Encoding.UTF8, "application/json")
                };

                return KifaServiceRestClient.Client.GetObject<KifaActionResult>(request) ??
                       KifaActionResult.UnknownError;
            },
            (ex, i) => HandleException(ex, i,
                $"Failure in DELETE {ModelId}({string.Join(", ", ids)})")));

    public KifaActionResult Call(string action, object? parameters = null)
        => KifaActionResult.FromAction(() => Call<object>(action, parameters));

    public TResponse Call<TResponse>(string action, object? parameters = null) {
        return Retry.Run(() => {
            var request = new HttpRequestMessage(HttpMethod.Post, GetUrl($"${action}"));

            if (parameters != null) {
                request.Content = new StringContent(
                    JsonConvert.SerializeObject(parameters, KifaJsonSerializerSettings.Default),
                    Encoding.UTF8, "application/json");
            }

            var result =
                KifaServiceRestClient.Client.GetObject<KifaActionResult<TResponse>>(request);
            if (result is {
                    Status: KifaActionStatus.OK
                }) {
                return result.Response!;
            }

            throw new KifaActionFailedException(result ?? KifaActionResult.UnknownError);
        }, (ex, i) => HandleException(ex, i, $"Failure in CALL {ModelId}.{action}"));
    }

    string GetUrl(string path, List<string>? parameters = null, KifaDataOptions? options = null) {
        parameters ??= [];
        if (options != null) {
            parameters.AddRange(options.GetUrlParameters());
        }

        return $"{KifaServiceRestClient.ServerAddress}/{ModelId}/" + path + (parameters.Count > 0
            ? $"?{parameters.JoinBy("&")}"
            : "");
    }

    static void HandleException(Exception ex, int index, string message) {
        if (index >= 5 || ex is KifaActionFailedException || ex is HttpRequestException {
                InnerException: SocketException {
                    Message: "Device not configured"
                }
            } || ex is HttpRequestException {
                StatusCode: HttpStatusCode.NotFound
            }) {
            throw ex;
        }

        KifaServiceRestClient.Logger.Warn(ex, $"{message} ({index})");
        Thread.Sleep(TimeSpan.FromSeconds(5));
    }

    static CacheControlHeaderValue GetCacheHeaderValue(bool? refresh)
        => CacheControlHeaderValue.Parse(refresh == true ? "no-cache" :
            refresh == false ? "only-if-cached" : "");
}
