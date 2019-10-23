using Microsoft.AspNetCore.Mvc;
using Pimix.Games.BreathOfTheWild;
using Pimix.Service;

namespace Pimix.Web.Api.Controllers.botw {
    [Route("api/" + KorokSeed.ModelId)]
    public class KorokSeedController : PimixController<KorokSeed> {
        static readonly PimixServiceClient<KorokSeed> client = new PimixServiceJsonClient<KorokSeed>();

        protected override PimixServiceClient<KorokSeed> Client => client;
    }
}
