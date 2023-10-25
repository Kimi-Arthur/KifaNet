using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Kifa.Web.Api.Controllers;

public class KifaDataController<TDataModel, TServiceClient> : ControllerBase
    where TDataModel : DataModel, WithModelId<TDataModel>, new()
    where TServiceClient : KifaServiceJsonClient<TDataModel>, new() {
    protected readonly TServiceClient Client = new();

    // GET api/values
    [HttpGet]
    public ActionResult<SortedDictionary<string, TDataModel>> List() => Client.List();

    // GET api/values/$
    [HttpGet("$")]
    public ActionResult<List<TDataModel?>> Get([FromBody] List<string> ids) => Client.Get(ids);

    // GET api/values/5
    [HttpGet("{id}")]
    public virtual ActionResult<TDataModel?> Get(string id, bool refresh = false) {
        id = Uri.UnescapeDataString(id);
        if (id.StartsWith("$")) {
            return new NotFoundResult();
        }

        return Client.Get(id, refresh);
    }

    // PATCH api/values/5
    [HttpPatch("{id}")]
    public KifaApiActionResult Patch(string id, [FromBody] TDataModel value) {
        value.Id = Uri.UnescapeDataString(id);
        value.Metadata = null;
        return Client.Update(value);
    }

    // PATCH api/values/$
    [HttpPatch("$")]
    public KifaApiActionResult Patch([FromBody] List<TDataModel> values)
        => Client.Update(values.Select(v => {
            v.Metadata = null;
            return v;
        }).ToList());

    // POST api/values/5
    [HttpPost("{id}")]
    public KifaApiActionResult Post(string id, [FromBody] TDataModel value) {
        value.Id = Uri.UnescapeDataString(id);
        value.Metadata = null;
        return Client.Set(value);
    }

    // POST api/values/$
    [HttpPost("$")]
    public KifaApiActionResult Post([FromBody] List<TDataModel> values)
        => Client.Set(values.Select(v => {
            v.Metadata = null;
            return v;
        }).ToList());

    [HttpPost("^")]
    public KifaApiActionResult Link([FromBody] List<string> ids)
        => new KifaBatchActionResult().AddRange(ids.Skip(1)
            .Select(id => (id, Client.Link(ids[0], id))));

    // DELETE api/values/5
    [HttpDelete("{id}")]
    public KifaApiActionResult Delete(string id) => Client.Delete(Uri.UnescapeDataString(id));

    // DELETE api/values/$
    [HttpDelete("$")]
    public KifaApiActionResult Delete([FromBody] List<string> ids) => Client.Delete(ids);

    [HttpGet("$fix")]
    [HttpPost("$fix")]
    public KifaApiActionResult Fix([FromBody] FixOptions? options)
        => KifaActionResult.FromAction(() => Client.FixVirtualLinks(options));
}

public class KifaApiActionResult : IConvertToActionResult {
    KifaActionResult ActionResult { get; set; }

    public static implicit operator KifaApiActionResult(KifaActionResult actionResult)
        => new() {
            ActionResult = actionResult
        };

    public IActionResult Convert()
        => ((IConvertToActionResult) new ActionResult<KifaActionResult>(ActionResult)).Convert();
}

public class KifaApiActionResult<TValue> : IConvertToActionResult {
    KifaActionResult<TValue> ActionResult { get; set; }

    public static implicit operator KifaApiActionResult<TValue>(TValue value)
        => new() {
            ActionResult = new KifaActionResult<TValue>(value)
        };

    public static implicit operator KifaApiActionResult<TValue>(KifaActionResult<TValue> response)
        => new() {
            ActionResult = response
        };

    public IActionResult Convert()
        => ((IConvertToActionResult) new ActionResult<KifaActionResult<TValue>>(ActionResult))
            .Convert();
}
