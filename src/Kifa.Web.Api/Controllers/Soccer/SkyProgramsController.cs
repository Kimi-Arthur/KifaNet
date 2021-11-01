using Kifa.SkyCh;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Soccer {
    [Route("api/" + SkyProgram.ModelId)]
    public class SkyProgramsController : KifaDataController<SkyProgram, SkyProgramJsonServiceClient> {
    }

    public class SkyProgramJsonServiceClient : KifaServiceJsonClient<SkyProgram>, SkyProgramServiceClient {
    }
}
