using Kifa.Memrise;
using Kifa.Music;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Music {
    [Route("api/" + MemriseCourse.ModelId)]
    public class GuitarChordController : KifaDataController<GuitarChord, GuitarChordJsonServiceClient> {
        protected override bool ShouldAutoRefresh => false;
    }

    public class GuitarChordJsonServiceClient : KifaServiceJsonClient<GuitarChord>, GuitarChordServiceClient {
    }
}
