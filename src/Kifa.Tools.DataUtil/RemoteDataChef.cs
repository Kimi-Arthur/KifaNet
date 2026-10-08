using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.DataUtil;

public class RemoteDataChef : DataChef {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public string ModelId { get; }

    public RemoteDataChef(string modelId) {
        ModelId = modelId;
    }

    public List<object> Load(string data) {
        var obj = DataChef.Deserializer.Deserialize<object>(data);
        return obj switch {
            List<object> list => list,
            Dictionary<object, object> dict => [dict],
            _ => []
        };
    }

    public KifaActionResult Import(string data) {
        var items = Load(data);
        var ids = items.Select(GetId).Where(id => id != null).ToList();

        return Logger.LogResult(Update(items),
            $"updating {ModelId}({string.Join(", ", ids)})");
    }

    KifaActionResult Update(List<object> data)
        => KifaActionResult.FromAction(() => Retry.Run(() => {
                var request = new HttpRequestMessage(new HttpMethod("PATCH"),
                    KifaServiceRestClient.FormatUrl(ModelId, "$")) {
                    Content = new StringContent(data.ToJson(), Encoding.UTF8, "application/json")
                };

                return KifaServiceRestClient.Client.GetObject<KifaBatchActionResult>(request) ??
                       KifaActionResult.UnknownError();
            },
            (ex, i) => KifaServiceRestClient.HandleException(ex, i,
                $"Failure in PATCH {ModelId}({string.Join(", ", data.Select(GetId))})")));

    public string Save(List<object> items, bool compact) {
        var serializer = compact ? DataChef.CompactSerializer : DataChef.Serializer;

        return
            $"# {ModelId}\n{string.Join("\n", items.Select(item => serializer.Serialize(new List<object> { item })))}";
    }

    public KifaActionResult<string> Export(string data, bool getAll, bool compact) {
        try {
            var items = Load(data);
            var ids = items.Select(GetId).Where(id => id != null).Select(id => id!).ToList();

            var updatedItems = getAll ? List(ids) : Get(ids);

            return new KifaActionResult<string>(Save(updatedItems, compact));
        } catch (KifaActionFailedException ex) {
            return new KifaActionResult<string> {
                Status = ex.ActionResult.Status,
                Message = ex.ActionResult.Message
            };
        } catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest) {
            return new KifaActionResult<string> {
                Status = KifaActionStatus.BadRequest,
                Message = ex.Message
            };
        } catch (Exception ex) {
            return new KifaActionResult<string> {
                Status = KifaActionStatus.Error,
                Message = ex.ToString()
            };
        }
    }

    List<object> List(List<string> existingIds)
        => Retry.Run(() => {
            var request = new HttpRequestMessage(HttpMethod.Get,
                KifaServiceRestClient.FormatUrl(ModelId, "", [("recursive", true)]));

            var response = KifaServiceRestClient.Client.Send(request);
            var content = response.GetString();
            var dict = DataChef.Deserializer.Deserialize<SortedDictionary<string, object>>(content) ?? [];

            var orderedDict = new SortedDictionary<string, object>();
            foreach (var (key, value) in dict) {
                orderedDict[key] = EnsureId(value, key);
            }

            return existingIds.Select(orderedDict.Pop)
                .Where(item => item != null)
                .Select(item => item!)
                .Concat(orderedDict.Values)
                .ToList();
        }, (ex, i) => KifaServiceRestClient.HandleException(ex, i, $"Failure in LIST {ModelId}"));

    List<object> Get(List<string> ids)
        => ids.Count != 0
            ? Retry.Run(() => {
                var request = new HttpRequestMessage(HttpMethod.Get,
                    KifaServiceRestClient.FormatUrl(ModelId, "$")) {
                    Content = new StringContent(ids.ToJson(), Encoding.UTF8, "application/json")
                };

                var response = KifaServiceRestClient.Client.Send(request);
                var content = response.GetString();
                var list = DataChef.Deserializer.Deserialize<List<object?>>(content) ?? [];

                var result = new List<object>();
                for (var i = 0; i < list.Count; i++) {
                    var item = list[i];
                    if (item != null) {
                        var id = i < ids.Count ? ids[i] : (GetId(item) ?? "");
                        result.Add(EnsureId(item, id));
                    }
                }

                return result;
            }, (ex, i) => KifaServiceRestClient.HandleException(ex, i,
                $"Failure in GET {ModelId}({string.Join(", ", ids)})"))
            : [];

    public KifaActionResult Link(string target, string link)
        => KifaActionResult.FromAction(() => Retry.Run(() => {
            var request = new HttpRequestMessage(HttpMethod.Post,
                KifaServiceRestClient.FormatUrl(ModelId, "^")) {
                Content = new StringContent(new List<string> {
                    target,
                    link
                }.ToJson(), Encoding.UTF8, "application/json")
            };

            return KifaServiceRestClient.Client.GetObject<KifaActionResult>(request) ??
                   KifaActionResult.UnknownError();
        }, (ex, i) => KifaServiceRestClient.HandleException(ex, i,
            $"Failure in LINK {ModelId}({link}) to {ModelId}({target})")));

    public KifaActionResult Delete(List<string> ids)
        => KifaActionResult.FromAction(() => Retry.Run(() => {
            var request = new HttpRequestMessage(HttpMethod.Delete,
                KifaServiceRestClient.FormatUrl(ModelId, "$")) {
                Content = new StringContent(ids.ToJson(), Encoding.UTF8, "application/json")
            };

            return KifaServiceRestClient.Client.GetObject<KifaBatchActionResult>(request) ??
                   KifaActionResult.UnknownError();
        }, (ex, i) => KifaServiceRestClient.HandleException(ex, i,
            $"Failure in DELETE {ModelId}({string.Join(", ", ids)})")));

    public KifaActionResult Call(string action, string? data = null) {
        var param = data == null || string.IsNullOrWhiteSpace(data)
            ? null
            : DataChef.Deserializer.Deserialize<object>(data);
        return KifaActionResult.FromAction(() => Retry.Run(() => {
            var request = new HttpRequestMessage(HttpMethod.Post,
                KifaServiceRestClient.FormatUrl(ModelId, $"${action}"));

            if (param != null) {
                request.Content = new StringContent(param.ToJson(), Encoding.UTF8,
                    "application/json");
            }

            return KifaServiceRestClient.Client.GetObject<KifaBatchActionResult>(request) ??
                   KifaActionResult.UnknownError();
        }, (ex, i) => KifaServiceRestClient.HandleException(ex, i,
            $"Failure in CALL {ModelId}.{action}")));
    }

    static string? GetId(object item) {
        if (item is IDictionary dict) {
            foreach (var key in dict.Keys) {
                if (key != null && string.Equals(key.ToString(), "Id", StringComparison.OrdinalIgnoreCase)) {
                    return dict[key]?.ToString();
                }
            }
        }

        return null;
    }

    static object EnsureId(object item, string id) {
        if (item is IDictionary dict) {
            var newDict = new Dictionary<object, object?> {
                ["Id"] = id
            };

            foreach (var key in dict.Keys) {
                if (key is object nonNullKey &&
                    !string.Equals(nonNullKey.ToString(), "Id", StringComparison.OrdinalIgnoreCase)) {
                    newDict[nonNullKey] = dict[nonNullKey];
                }
            }

            return newDict;
        }

        return item;
    }
}
