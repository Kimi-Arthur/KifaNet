using Kifa.Soccer;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Soccer;

[Route("api/" + Team.ModelId)]
public class TeamsController : KifaDataController<Team, TeamJsonServiceClient> {
}

public class TeamJsonServiceClient : KifaServiceJsonClient<Team>, TeamServiceClient {
}
