using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kifa.Api.Files;
using Kifa.IO;
using Kifa.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace Kifa.Web.Api.Controllers;

public class
    FilesController : KifaDataController<FileInformation, FileInformationJsonServiceClient> {
    static readonly FileExtensionContentTypeProvider provider = new();

    public class ListFolderRequest {
        public string Folder { get; set; }
        public bool Recursive { get; set; } = false;
    }

    [HttpGet("$get_folder")]
    public KifaActionResult<List<FolderInfo>> GetFolder(string folder, List<string> targets) {
        return Client.GetFolder(folder, targets);
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

    public class DeleteServerRequest {
        public string? ServerName { get; set; }
        public string? ServerType { get; set; }
    }

    [HttpPost("$delete_server")]
    public KifaApiActionResult DeleteServer([FromBody] DeleteServerRequest request)
        => Client.DeleteServer(request.ServerName, request.ServerType);

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
    public List<FolderInfo> GetFolder(string folder, List<string> targets) {
        if (!folder.EndsWith('/')) {
            folder += "/";
        }

        var files = List(folder.Trim('/'));
        var folders = new Dictionary<string, FolderInfo>();

        var topFolder = CreateNewFolder(folder, targets);
        folders.Add(folder, topFolder);

        foreach (var file in files.Values) {
            var sha256 = file.Sha256;
            var size = file.Size;
            if (sha256 == null || size == null) {
                // TODO: Notify or throw here.
                continue;
            }

            var folderName = file.Id[..(file.Id + "/").IndexOf('/', folder.Length)];
            if (!folders.TryGetValue(folderName, out var folderStat)) {
                folderStat = CreateNewFolder(folderName, targets);

                folders.Add(folderName, folderStat);
            }

            folderStat.Overall.AddFile(sha256, size.Value);
            topFolder.Overall.AddFile(sha256, size.Value);
            foreach (var target in targets) {
                if (file.Locations.Any(kv => kv.Key.StartsWith(target) && kv.Value != null)) {
                    folderStat.Stats[target].AddFile(sha256, size.Value);
                    topFolder.Stats[target].AddFile(sha256, size.Value);
                }
            }
        }

        return folders.Values.OrderByDescending(f => f.GetMissingSizes(targets),
            Comparer<List<long>>.Create((first, second) => {
                // Stupid usage of LINQ for a single for-loop.
                var item = first.Zip(second).Where(x => x.First != x.Second).FirstOrDefault((0, 0));
                return item.First.CompareTo(item.Second);
            })).ThenBy(f => f.Folder.GetNaturalSortKey()).ToList();
    }

    static FolderInfo CreateNewFolder(string folderName, List<string> targets) {
        var folderStat = new FolderInfo {
            Folder = folderName,
            Stats = []
        };

        foreach (var target in targets) {
            folderStat.Stats.Add(target, new FileStat());
        }

        return folderStat;
    }

    public List<string> ListFolder(string folder, bool recursive = false) {
        return List(folder.Trim('/'), recursive).Keys.OrderBy(i => i.GetNaturalSortKey()).ToList();
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

        if (file.Locations.ContainsKey(location)) {
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
        => new KifaBatchActionResult().AddRange(List().Values.AsParallel().Select(file => {
            if (file.Locations.Count > 0) {
                var locationsFromServer = file.Locations
                    .Where(l => ((FileLocation) l.Key).Server == fromServer).ToList();
                if (locationsFromServer.Count == 0) {
                    return (file.Id, new KifaActionResult {
                        Status = KifaActionStatus.OK,
                        Message = $"No files to move for {file.Id}."
                    });
                }

                var message = string.Join("\n", locationsFromServer.Select(location => {
                    var newLocation = (FileLocation) location.Key;
                    newLocation.Server = toServer;

                    file.Locations.Remove(location.Key);
                    file.Locations[newLocation.ToString()] = Kifa.Max(location.Value,
                        file.Locations.GetValueOrDefault(newLocation.ToString()));
                    return $"\tMoved {location.Key} to {newLocation}";
                }));

                Update(file);

                return (file.Id, new KifaActionResult {
                    Status = KifaActionStatus.OK,
                    Message =
                        $"Moved {locationsFromServer.Count} files to {toServer} for {file.Id}:\n{message}"
                });
            }

            return (file.Id, new KifaActionResult {
                Status = KifaActionStatus.OK,
                Message = $"No files to move for {file.Id}."
            });
        }));

    public KifaApiActionResult DeleteServer(string? serverName, string? serverType)
        => new KifaBatchActionResult().AddRange(List().Values.AsParallel().Select(file => {
            if (file.Locations.Count > 0) {
                var locationsOnServer = file.Locations.Where(l
                    => ((FileLocation) l.Key).Server == serverName ||
                       ((FileLocation) l.Key).ServerType == serverType).ToList();
                if (locationsOnServer.Count == 0) {
                    return (file.Id, new KifaActionResult {
                        Status = KifaActionStatus.OK,
                        Message = $"No files to move for {file.Id}."
                    });
                }

                var message = string.Join("\n", locationsOnServer.Select(location => {
                    file.Locations.Remove(location.Key);
                    return $"\tRemoved {location.Key}.";
                }));

                Update(file);

                return (file.Id, new KifaActionResult {
                    Status = KifaActionStatus.OK,
                    Message = $"Removed {locationsOnServer.Count} files for {file.Id}:\n{message}"
                });
            }

            return (file.Id, new KifaActionResult {
                Status = KifaActionStatus.OK,
                Message = $"No files to remove for {file.Id}."
            });
        }));
}
