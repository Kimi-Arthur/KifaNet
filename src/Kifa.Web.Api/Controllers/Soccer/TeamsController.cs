using Kifa.Soccer;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Soccer;


public class TeamsController : KifaDataController<Team, TeamJsonServiceClient> {
}

public class TeamJsonServiceClient : KifaServiceJsonClient<Team>, TeamServiceClient {
}
