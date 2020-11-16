using Microsoft.AspNetCore.Mvc;
using Pimix.Games.BreathOfTheWild;

namespace Pimix.Web.Api.Controllers.botw {
    [Route("api/" + KorokSeed.ModelId)]
    public class KorokSeedController : PimixController<KorokSeed, PimixServiceJsonClient<KorokSeed>> {
    }

    [Route("api/" + Game.ModelId)]
    public class BotwGameController : PimixController<Game, PimixServiceJsonClient<Game>> {
    }
}
