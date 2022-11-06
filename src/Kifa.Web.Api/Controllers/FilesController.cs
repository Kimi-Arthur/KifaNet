using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kifa.Api.Files;
using Kifa.IO;
using Kifa.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using NLog;

namespace Kifa.Web.Api.Controllers;

[Route("api/" + FileInformation.ModelId)]
public class
    FilesController : KifaDataController<FileInformation, FileInformationJsonServiceClient> {
    static readonly FileExtensionContentTypeProvider provider = new();

    public class ListFolderRequest {
        public string Folder { get; set; }
        public bool Recursive { get; set; } = false;
    }

    [HttpGet("$list_folder")]
    public KifaApiActionResult<List<string>> ListFolderGet([FromQuery] ListFolderRequest request)
        => Client.ListFolder(request.Folder, request.Recursive);

    [HttpPost("$list_folder")]
    public KifaApiActionResult<List<string>> ListFolderPost([FromBody] ListFolderRequest request)
        => Client.ListFolder(request.Folder, request.Recursive);

    public class MoveServerRequest {
        #region public late string FromServer { get; set; }

        string? fromServer;

        public string FromServer {
            get => Late.Get(fromServer);
            set => Late.Set(ref fromServer, value);
        }

        #endregion

        #region public late string ToServer { get; set; }

        string? toServer;

        public string ToServer {
            get => Late.Get(toServer);
            set => Late.Set(ref toServer, value);
        }

        #endregion
    }

    [HttpPost("$move_server")]
    public KifaApiActionResult MoveServer([FromBody] MoveServerRequest request)
        => Client.MoveServer(request.FromServer, request.ToServer);

    public class AddLocationRequest {
        public string Id { get; set; }
        public string Location { get; set; }
        public bool Verified { get; set; }
    }

    [HttpPost("$add_location")]
    public KifaApiActionResult AddLocation([FromBody] AddLocationRequest request)
        => Client.AddLocation(request.Id, request.Location, request.Verified);

    public class RemoveLocationRequest {
        public string Id { get; set; }
        public string Location { get; set; }
    }

    [HttpPost("$remove_location")]
    public KifaApiActionResult RemoveLocation([FromBody] RemoveLocationRequest request)
        => Client.RemoveLocation(request.Id, request.Location);

    [HttpGet("$stream")]
    public FileStreamResult Stream(string id) {
        id = Uri.UnescapeDataString(id);
        if (!provider.TryGetContentType(id, out var contentType)) {
            contentType = "application/octet-stream";
        }

        return new FileStreamResult(
            new KifaFile(Client.Get(id).Locations.Keys.First(x => x.StartsWith("google")))
                .OpenRead(), contentType) {
            FileDownloadName = id.Substring(id.LastIndexOf('/') + 1),
            EnableRangeProcessing = true
        };
    }
}

public class FileInformationJsonServiceClient : KifaServiceJsonClient<FileInformation>,
    FileInformationServiceClient {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static readonly Dictionary<string, long> ShardSizes = new() {
        ["swiss"] = 1 << 30
    };

    public List<string> ListFolder(string folder, bool recursive = false) {
        var prefix = $"{KifaServiceJsonClient.DataFolder}/{ModelId}";
        folder = $"{prefix}/{folder.Trim('/')}";
        Logger.Trace($"Listing items in folder {folder}...");
        if (!Directory.Exists(folder)) {
            if (File.Exists(folder + ".json")) {
                Logger.Trace($"{folder} is actually a file. Return one element instead.");
                return new List<string> {
                    folder[prefix.Length..]
                };
            }

            Logger.Trace($"{folder} has no items.");
            return new List<string>();
        }

        var directory = new DirectoryInfo(folder);
        var items = directory.GetFiles("*.json",
            recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        return items.Select(i => i.FullName[prefix.Length..^5]).OrderBy(i => i.GetNaturalSortKey())
            .ToList();
    }

    public KifaActionResult AddLocation(string id, string location, bool verified = false) {
        var file = Get(id);
        if (file == null) {
            return new KifaActionResult {
                Status = KifaActionStatus.BadRequest,
                Message = $"Cannot find {id}"
            };
        }

        file.Locations[location] =
            verified ? DateTime.UtcNow : file.Locations.GetValueOrDefault(location);
        return Update(file);
    }

    public KifaActionResult RemoveLocation(string id, string location) {
        var file = Get(id);
        if (file == null) {
            return new KifaActionResult {
                Status = KifaActionStatus.Warning,
                Message = $"Cannot find {id}."
            };
        }

        if (file.Locations.GetValueOrDefault(location) != null) {
            file.Locations.Remove(location);
            var update = Update(file);
            return new KifaActionResult {
                Status = update.Status,
                Message = update.Status == KifaActionStatus.OK
                    ? $"Removed location {location} from {id}."
                    : update.Message
            };
        }

        return new KifaActionResult {
            Status = KifaActionStatus.Warning,
            Message = $"Location {location} not found for {id}."
        };
    }

    public string? CreateLocation(string id, string type = "google", string format = "v1") {
        var file = Get(id);

        if (file.Size == null || file.Sha256 == null) {
            // TODO: throw
            return null;
        }

        var path = $"/$/{file.Sha256}.{format}";

        return file.Locations.Keys.FirstOrDefault(location
            => location.StartsWith($"{type}:") && location.EndsWith(path)) ?? type switch {
            "google" => $"google:good{path}",
            "swiss" => $"swiss:s0000{path}",
            _ => null
        };
    }

    public string GetLocation(string id, List<string> types = null)
        => throw new NotImplementedException();

    public KifaApiActionResult MoveServer(string fromServer, string toServer)
        => List().Values.AsParallel().Select(file => {
            if (file.Locations.Count > 0) {
                var locationsFromServer = file.Locations
                    .Where(l => new FileLocation(l.Key).Server == fromServer).ToList();
                if (locationsFromServer.Count == 0) {
                    return new KifaActionResult {
                        Status = KifaActionStatus.OK,
                        Message = $"No files to move for {file.Id}."
                    };
                }

                var message = string.Join("\n", locationsFromServer.Select(location => {
                    var newLocation = new FileLocation(location.Key) {
                        Server = toServer
                    };

                    file.Locations.Remove(location.Key);
                    file.Locations[newLocation.ToString()] = Kifa.Max(location.Value,
                        file.Locations.GetValueOrDefault(newLocation.ToString()));
                    return $"\tMoved {location.Key} to {newLocation}";
                }));

                Update(file);

                return new KifaActionResult {
                    Status = KifaActionStatus.OK,
                    Message =
                        $"Moved {locationsFromServer.Count} files to {toServer} for {file.Id}:\n{message}"
                };
            }

            return new KifaActionResult {
                Status = KifaActionStatus.OK,
                Message = $"No files to move for {file.Id}."
            };
        }).Aggregate(new KifaBatchActionResult(),
            (result, actionResult) => result.Add(actionResult));
}
