using Microsoft.AspNetCore.Mvc;
using Pimix.Games.BreathOfTheWild;
using Pimix.Service;

namespace Pimix.Web.Api.Controllers.botw {
    [Route("api/" + KorokSeed.ModelId)]
    public class KorokSeedController : PimixController<KorokSeed> {
        static readonly PimixServiceClient<KorokSeed> client = new PimixServiceJsonClient<KorokSeed>();

        protected override PimixServiceClient<KorokSeed> Client => client;
    }

    [Route("api/" + Game.ModelId)]
    public class BotwGameController : PimixController<Game> {
        static readonly PimixServiceClient<Game> client = new PimixServiceJsonClient<Game>();

        protected override PimixServiceClient<Game> Client => client;
    }
}
