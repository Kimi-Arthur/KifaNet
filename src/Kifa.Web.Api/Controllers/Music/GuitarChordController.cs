using Kifa.Music;
using Microsoft.AspNetCore.Mvc;
using Svg;

namespace Kifa.Web.Api.Controllers.Music {
    [Route("api/" + GuitarChord.ModelId)]
    public class GuitarChordController : KifaDataController<GuitarChord, GuitarChordJsonServiceClient> {
        protected override bool ShouldAutoRefresh => false;

        [HttpGet("$get_picture")]
        [HttpPost("$get_picture")]
        public KifaApiActionResult<string> GetPicture(string id) => Client.GetPicture(id).GetXML();
    }

    public class GuitarChordJsonServiceClient : KifaServiceJsonClient<GuitarChord>, GuitarChordServiceClient {
        public SvgDocument GetPicture(string id) => Get(id).GetPicture();
    }
}
