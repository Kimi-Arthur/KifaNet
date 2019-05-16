using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Pimix.Api.Files;
using Pimix.IO;
using Pimix.Service;

namespace Pimix.Web.Api.Controllers {
    public class FilesController : PimixController<FileInformation> {
        static readonly FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();
        static FileInformationServiceClient client = new FileInformationJsonServiceClient();

        protected override PimixServiceClient<FileInformation> Client => client;

        [HttpGet("$list_folder")]
        public ActionResult<List<string>> ListFolder(string folder, bool recursive) {
            return client.ListFolder(folder, recursive);
        }

        [HttpGet("$stream")]
        public FileStreamResult Stream(string id) {
            id = Uri.UnescapeDataString(id);
            if (!provider.TryGetContentType(id, out var contentType)) {
                contentType = "application/octet-stream";
            }

            return new FileStreamResult(new PimixFile(client.Get(id).Locations.Keys.First(x => x.StartsWith("google")))
                .OpenRead(), contentType) {
                FileDownloadName = id.Substring(id.LastIndexOf('/') + 1),
                EnableRangeProcessing = true
            };
        }
    }
    
    public class FileInformationJsonServiceClient : PimixServiceJsonClient<FileInformation>,
        FileInformationServiceClient {
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
    }
}
