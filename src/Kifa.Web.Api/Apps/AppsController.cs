using System;
using System.Collections.Generic;
using Kifa.Service;
using Kifa.Web.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Apps;

[Route("apps/{app}/{user}")]
public class AppsController : ControllerBase {
    readonly KifaServiceJsonClient<AppData> client = new();

    [HttpGet]
    public KifaApiActionResult<Dictionary<string, object>> GetData(string app, string user) {
        var id = $"{app}/{user}";
        var data = client.Get(id);
        if (data == null) {
            return new KifaActionResult<Dictionary<string, object>> {
                Status = KifaActionStatus.BadRequest,
                Message = $"Data with id {id} is not found."
            };
        }

        return new KifaActionResult<Dictionary<string, object>> {
            Response = data.Data,
            Status = KifaActionStatus.OK
        };
    }

    [HttpPost]
    public KifaApiActionResult PostData(string app, string user,
        [FromBody] Dictionary<string, object> data) {
        return client.Set(new AppData {
            Id = $"{app}/{user}",
            LastUpdate = DateTimeOffset.UtcNow,
            Data = data
        });
    }
}
