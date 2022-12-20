using Kifa.Games.BreathOfTheWild;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.botw;

public class KorokSeedController : KifaDataController<KorokSeed, KifaServiceJsonClient<KorokSeed>> {
}

public class BotwGameController : KifaDataController<Game, KifaServiceJsonClient<Game>> {
}
