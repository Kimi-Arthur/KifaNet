using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NLog;
using Pimix.Service;

namespace Pimix.Web.Api {
    public class PimixServiceJsonClient {
        public static string DataFolder { get; set; }
    }

    public class PimixServiceJsonClient<TDataModel> : BasePimixServiceClient<TDataModel>
        where TDataModel : DataModel, new() {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        Dictionary<string, List<string>> Groups { get; } = new();

        public override SortedDictionary<string, TDataModel> List() {
            var prefix = $"{PimixServiceJsonClient.DataFolder}/{modelId}";
            if (!Directory.Exists(prefix)) {
                return new SortedDictionary<string, TDataModel>();
            }

            var directory = new DirectoryInfo(prefix);
            var items = directory.GetFiles("*.json", SearchOption.AllDirectories);
            return new SortedDictionary<string, TDataModel>(items.Select(i => {
                using var reader = i.OpenText();
                return JsonConvert.DeserializeObject<TDataModel>(reader.ReadToEnd(), Defaults.JsonSerializerSettings);
            }).ToDictionary(i => i.Id, i => i));
        }

        public override TDataModel Get(string id) {
            var data = Read(id);
            if (data.Metadata?.Id != null) {
                data = Read(data.Metadata.Id);
                data.Metadata.Id = data.Id;
                data.Id = id;
            }

            return data;
        }

        public override List<TDataModel> Get(List<string> ids) => ids.Select(Get).ToList();

        public override RestActionResult Set(TDataModel data) =>
            RestActionResult.FromAction(() => {
                data.Metadata ??= Get(data.Id).Metadata;
                if (data.Metadata?.Id != null) {
                    // The data is linked.
                    data.Id = data.Metadata.Id;
                    data.Metadata.Id = null;
                }

                Write(data);
            });

        public override RestActionResult Update(TDataModel data) =>
            RestActionResult.FromAction(() => {
                var original = Get(data.Id);
                JsonConvert.PopulateObject(JsonConvert.SerializeObject(data, Defaults.JsonSerializerSettings),
                    original);
                if (data.Metadata?.Id != null) {
                    // The data is linked.
                    data.Id = data.Metadata.Id;
                    data.Metadata.Id = null;
                }

                Write(data);
            });

        public override RestActionResult Delete(string id) {
            throw new NotImplementedException();
        }

        public override RestActionResult Link(string targetId, string linkId) {
            var target = Get(targetId);
            var link = Get(linkId);

            if (target.Id == null) {
                return LogAndReturn(new RestActionResult {
                    Status = RestActionStatus.BadRequest, Message = $"Target {targetId} doesn't exist."
                });
            }

            var realTargetId = target.Metadata?.Id ?? target.Id;

            if (link.Id != null) {
                var realLinkId = link.Metadata?.Id ?? link.Id;
                if (realLinkId == realTargetId) {
                    return LogAndReturn(new RestActionResult {
                        Status = RestActionStatus.BadRequest,
                        Message = $"Link {linkId} ({realLinkId}) is already linked to {targetId} ({realTargetId})."
                    });
                }

                return LogAndReturn(new RestActionResult {
                    Status = RestActionStatus.BadRequest,
                    Message = $"Both {linkId} ({realLinkId}) and {targetId} ({realTargetId}) have data populated."
                });
            }

            Write(new TDataModel {Id = linkId, Metadata = new DataMetadata {Id = realTargetId}});

            target.Metadata ??= new DataMetadata();
            target.Metadata.Links ??= new HashSet<string>();
            target.Metadata.Links.Add(linkId);
            target.Id = realTargetId;
            target.Metadata.Id = null;
            Write(target);
            return RestActionResult.SuccessResult;
        }

        public override RestActionResult Refresh(string id) {
            var value = Get(id);
            value.Fill();
            return Set(value);
        }

        TDataModel Read(string id) {
            return JsonConvert.DeserializeObject<TDataModel>(ReadRaw(id), Defaults.JsonSerializerSettings);
        }

        void Write(TDataModel data) {
            var path = $"{PimixServiceJsonClient.DataFolder}/{modelId}/{data.Id.Trim('/')}.json";
            MakeParent(path);
            File.WriteAllText(path, JsonConvert.SerializeObject(data, Defaults.PrettyJsonSerializerSettings) + "\n");
        }

        string ReadRaw(string id) {
            var path = $"{PimixServiceJsonClient.DataFolder}/{modelId}/{id.Trim('/')}.json";
            return !File.Exists(path) ? "{}" : File.ReadAllText(path);
        }

        static RestActionResult LogAndReturn(RestActionResult result) {
            logger.Log(result.Status switch {
                RestActionStatus.Error => LogLevel.Error,
                RestActionStatus.BadRequest => LogLevel.Warn,
                RestActionStatus.OK => LogLevel.Info,
                _ => LogLevel.Info
            }, result.Message);
            return result;
        }

        static void MakeParent(string path) {
            Directory.CreateDirectory(path[..path.LastIndexOf('/')]);
        }
    }
}
