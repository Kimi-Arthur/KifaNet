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
    static readonly Logger logger = LogManager.GetCurrentClassLogger();

    static readonly Dictionary<string, long> ShardSizes = new() {
        ["swiss"] = 1 << 30
    };

    public List<string> ListFolder(string folder, bool recursive = false) {
        var prefix = $"{KifaServiceJsonClient.DataFolder}/{ModelId}";
        logger.Trace(prefix);
        folder = $"{prefix}/{folder.Trim('/')}";
        logger.Trace(folder);
        if (!Directory.Exists(folder)) {
            return new List<string>();
        }

        var directory = new DirectoryInfo(folder);
        var items = directory.GetFiles("*.json",
            recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        return items.Select(i
                => i.FullName.Substring(prefix.Length, i.FullName.Length - prefix.Length - 5))
            .OrderBy(i => i.GetNaturalSortKey()).ToList();
    }

    public KifaActionResult AddLocation(string id, string location, bool verified = false) {
        var file = Get(id);
        if (file == null) {
            return new KifaActionResult {
                Status = KifaActionStatus.BadRequest,
                Message = $"Cannot find {id}"
            };
        }

        file.Locations ??= new Dictionary<string, DateTime?>();
        file.Locations[location] =
            verified ? DateTime.UtcNow : file.Locations.GetValueOrDefault(location);
        return Update(file);
    }

    public KifaActionResult RemoveLocation(string id, string location) {
        var file = Get(id);
        if (file == null) {
            return new KifaActionResult {
                Status = KifaActionStatus.BadRequest,
                Message = $"Cannot find {id}"
            };
        }

        if (file.Locations != null) {
            file.Locations.Remove(location);
            return Update(file);
        }

        return new KifaActionResult {
            Status = KifaActionStatus.OK,
            Message = $"Location {location} not found for {id}."
        };
    }

    public string CreateLocation(string id, string type = "google", string format = "v1") {
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
}
