using System.IO;
using System.Text;
using Kifa.Music;
using Microsoft.AspNetCore.Mvc;
using Svg;

namespace Kifa.Web.Api.Controllers.Music;

public class GuitarChordController : KifaDataController<GuitarChord, GuitarChordJsonServiceClient> {
    [HttpGet("$get_picture")]
    public FileStreamResult GetPicture(string id)
        => new(new MemoryStream(new UTF8Encoding(false).GetBytes(Client.GetPicture(id).GetXML())),
            "image/svg+xml") {
            EnableRangeProcessing = true
        };
}

public class GuitarChordJsonServiceClient : KifaServiceJsonClient<GuitarChord>,
    GuitarChordServiceClient {
    public SvgDocument GetPicture(string id) => Get(id).GetPicture();
}
