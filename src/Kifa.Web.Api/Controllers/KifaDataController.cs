using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Kifa.Web.Api.Controllers;

[ApiController]
public abstract class KifaDataController<TDataModel, TServiceClient> : ControllerBase
    where TDataModel : DataModel, new()
    where TServiceClient : KifaServiceClient<TDataModel>, new() {
    static readonly TimeSpan MinRefreshInterval = TimeSpan.FromHours(1);

    static readonly TimeSpan[] RefreshIntervals = {
        TimeSpan.FromDays(1), TimeSpan.FromDays(10), TimeSpan.FromDays(40), TimeSpan.FromDays(400)
    };

    protected readonly TServiceClient Client = new();

    // Only need to override this if the DataModel can be fill()ed but should not be.
    protected virtual bool ShouldAutoRefresh => true;

    protected virtual bool AlwaysAutoRefresh => false;

    // GET api/values
    [HttpGet]
    public ActionResult<SortedDictionary<string, TDataModel>> List() => Client.List();

    // GET api/values/$
    [HttpGet("$")]
    public virtual ActionResult<List<TDataModel>> Get([FromBody] List<string> ids,
        [FromQuery] bool refresh = false) {
        return ids.Select(id => GetValue(id, refresh)).ToList();
    }

    // GET api/values/5
    [HttpGet("{id}")]
    public virtual ActionResult<TDataModel> Get(string id, [FromQuery] bool refresh = false) {
        id = Uri.UnescapeDataString(id);
        if (id.StartsWith("$")) {
            return new NotFoundResult();
        }

        return GetValue(id, refresh);
    }

    TDataModel? GetValue(string id, bool refresh) {
        var value = Client.Get(id);
        if (value?.Id == null || refresh || NeedRefresh(value)) {
            value ??= new TDataModel();
            value.Id ??= id;
            var updated = value.Fill();
            if (updated == null) {
                return value;
            }

            if (ShouldAutoRefresh) {
                value.Metadata ??= new DataMetadata();
                value.Metadata.Freshness ??= new FreshnessMetadata();
                value.Metadata.Freshness.LastRefreshed = DateTimeOffset.UtcNow;
                value.Metadata.Freshness.LastUpdated ??= value.Metadata.Freshness.LastRefreshed;
            }

            Client.Set(value);
        }

        return value;
    }

    bool NeedRefresh(TDataModel value) {
        if (AlwaysAutoRefresh) {
            return true;
        }

        if (!ShouldAutoRefresh) {
            return false;
        }

        if (value.Metadata?.Freshness?.LastRefreshed == null) {
            return true;
        }

        var stableDuration = value.Metadata.Freshness.LastRefreshed -
                             value.Metadata.Freshness.LastUpdated;
        var newDuration = DateTimeOffset.UtcNow - value.Metadata.Freshness.LastRefreshed;

        return RefreshIntervals.Reverse().FirstOrDefault(interval => interval < stableDuration)
            .Or(MinRefreshInterval) < newDuration;
    }

    // PATCH api/values/5
    [HttpPatch("{id}")]
    public KifaApiActionResult Patch(string id, [FromBody] TDataModel value) {
        value.Id ??= Uri.UnescapeDataString(id);
        value.Metadata = null;
        return Client.Update(value);
    }

    // PATCH api/values/$
    [HttpPatch("$")]
    public KifaApiActionResult Patch([FromBody] List<TDataModel> values) {
        foreach (var value in values) {
            value.Metadata = null;
        }

        return Client.Update(values);
    }

    // POST api/values/5
    [HttpPost("{id}")]
    public KifaApiActionResult Post(string id, [FromBody] TDataModel value) {
        value.Id ??= Uri.UnescapeDataString(id);
        value.Metadata = null;
        value.Fill();
        return Client.Set(value);
    }

    // POST api/values/$
    [HttpPost("$")]
    public KifaApiActionResult Post([FromBody] List<TDataModel> values) {
        foreach (var value in values) {
            value.Metadata = null;
            value.Fill();
        }

        return Client.Set(values);
    }

    [HttpPost("^")]
    public KifaApiActionResult Link([FromBody] List<string> ids)
        => ids.Skip(1)
            .Select(id => Client.Link(Uri.UnescapeDataString(ids[0]), Uri.UnescapeDataString(id)))
            .Aggregate(new KifaBatchActionResult(), (s, x) => s.Add(x));

    // DELETE api/values/5
    [HttpDelete("{id}")]
    public KifaApiActionResult Delete(string id) => Client.Delete(Uri.UnescapeDataString(id));

    // DELETE api/values/$
    [HttpDelete("$")]
    public KifaApiActionResult Delete([FromBody] List<string> ids) => Client.Delete(ids);

    // POST api/values/$refresh?id={id}
    // TODO: should be generated.
    [HttpGet("$refresh")]
    public KifaApiActionResult RefreshGet([FromQuery] RefreshRequest request) => Refresh(request);

    // POST api/values/$refresh?id={id}
    // TODO: should be generated.
    [HttpPost("$refresh")]
    public KifaApiActionResult RefreshPost([FromBody] RefreshRequest request) => Refresh(request);

    public class RefreshRequest {
        public string Id { get; set; }
    }

    // Action [HttpAction("$refresh")]
    // TODO: Should use the attribute above.
    public virtual KifaApiActionResult Refresh(RefreshRequest request) {
        if (request.Id == "$") {
            var result = new KifaBatchActionResult();
            foreach (var id in Client.List().Keys) {
                result.Add(Client.Refresh(id));
            }

            return result;
        }

        return Client.Refresh(request.Id);
    }
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

    public IActionResult Convert()
        => ((IConvertToActionResult) new ActionResult<KifaActionResult<TValue>>(ActionResult))
            .Convert();
}
