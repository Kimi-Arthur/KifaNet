using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Kifa.Api.Files;
using Kifa.IO;

namespace Kifa.Web.Api.Controllers {
    [Route("api/" + FileInformation.ModelId)]
    public class FilesController : KifaDataController<FileInformation, FileInformationJsonServiceClient> {
        static readonly FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

        [HttpGet("$list_folder")]
        public PimixActionResult<List<string>> ListFolder(string folder, bool recursive) =>
            Client.ListFolder(folder, recursive);

        [HttpGet("$stream")]
        public FileStreamResult Stream(string id) {
            id = Uri.UnescapeDataString(id);
            if (!provider.TryGetContentType(id, out var contentType)) {
                contentType = "application/octet-stream";
            }

            return new FileStreamResult(
                new KifaFile(Client.Get(id).Locations.Keys.First(x => x.StartsWith("google"))).OpenRead(),
                contentType) {FileDownloadName = id.Substring(id.LastIndexOf('/') + 1), EnableRangeProcessing = true};
        }
    }

    public class FileInformationJsonServiceClient : KifaServiceJsonClient<FileInformation>,
        FileInformationServiceClient {
        static readonly Dictionary<string, long> ShardSizes = new Dictionary<string, long> {["swiss"] = 1 << 30};

        public List<string> ListFolder(string folder, bool recursive = false) {
            var prefix = $"{PimixServiceJsonClient.DataFolder}/{modelId}";
            folder = $"{prefix}/{folder.TrimEnd('/')}";
            if (!Directory.Exists(folder)) {
                return new List<string>();
            }

            var directory = new DirectoryInfo(folder);
            var items = directory.GetFiles("*.json",
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            return items.Select(i => i.FullName.Substring(prefix.Length, i.FullName.Length - prefix.Length - 5))
                .ToList();
        }

        public void AddLocation(string id, string location, bool verified = false) {
            throw new NotImplementedException();
        }

        public void RemoveLocation(string id, string location) {
            throw new NotImplementedException();
        }

        public string CreateLocation(string id, string type = "google", string format = "v1") {
            var file = Get(id);

            if (file.Size == null || file.Sha256 == null) {
                // TODO: throw
                return null;
            }

            var path = $"/$/{file.Sha256}.{format}";

            return file.Locations.Keys.FirstOrDefault(location =>
                location.StartsWith($"{type}:") && location.EndsWith(path)) ?? type switch {
                "google" => $"google:good{path}",
                "swiss" => $"swiss:s0000{path}",
                _ => null
            };
        }

        public string GetLocation(string id, List<string> types = null) => throw new NotImplementedException();
    }
}
