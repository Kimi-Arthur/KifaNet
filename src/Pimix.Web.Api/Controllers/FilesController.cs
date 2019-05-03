using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Pimix.Api.Files;
using Pimix.IO;

namespace Pimix.Web.Api.Controllers {
    public class FilesController : PimixController<FileInformation> {
        static readonly FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

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
}
