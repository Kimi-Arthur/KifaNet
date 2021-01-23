using Microsoft.AspNetCore.Mvc;
using Pimix.Games.BreathOfTheWild;

namespace Pimix.Web.Api.Controllers.botw {
    [Route("api/" + KorokSeed.ModelId)]
    public class KorokSeedController : KifaDataController<KorokSeed, KifaServiceJsonClient<KorokSeed>> {
    }

    [Route("api/" + Game.ModelId)]
    public class BotwGameController : KifaDataController<Game, KifaServiceJsonClient<Game>> {
    }
}
