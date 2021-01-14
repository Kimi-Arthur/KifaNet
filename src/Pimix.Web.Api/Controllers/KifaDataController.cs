using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Newtonsoft.Json;
using Pimix.Service;

namespace Pimix.Web.Api.Controllers {
    [ApiController]
    public abstract class KifaDataController<TDataModel, TServiceClient> : ControllerBase where TDataModel : DataModel
        where TServiceClient : PimixServiceClient<TDataModel>, new() {
        protected static readonly TimeSpan[] WeeklyIntervals = {TimeSpan.FromDays(1), TimeSpan.FromDays(10)};

        protected readonly TServiceClient Client = new();
        protected virtual IEnumerable<TimeSpan> RefreshIntervals => null;

        // GET api/values
        [HttpGet]
        public ActionResult<SortedDictionary<string, TDataModel>> List() => Client.List();

        // GET api/values/5
        [HttpGet("{id}")]
        public virtual ActionResult<TDataModel> Get(string id, [FromQuery] bool refresh = false) {
            id = Uri.UnescapeDataString(id);
            if (id.StartsWith("$")) {
                return new NotFoundResult();
            }

            var value = Client.Get(id);
            if (refresh || value.Id == null || NeedRefresh(value)) {
                value.Id ??= id;
                if (RefreshIntervals != null) {
                    value.Metadata ??= new DataMetadata();
                    value.Metadata.LastRefreshed = DateTimeOffset.Now;
                    if (value.Fill() || value.Metadata.LastUpdated == null) {
                        value.Metadata.LastUpdated = value.Metadata.LastRefreshed;
                    }
                } else {
                    value.Fill();
                }

                Client.Set(value);
            }

            return value;
        }

        bool NeedRefresh(TDataModel value) {
            if (RefreshIntervals == null) {
                return false;
            }

            if (value.Metadata?.LastRefreshed == null) {
                return true;
            }

            var stableDuration = value.Metadata.LastRefreshed - value.Metadata.LastUpdated;
            var newDuration = DateTimeOffset.UtcNow - value.Metadata.LastRefreshed;

            return RefreshIntervals.Reverse().FirstOrDefault(interval => interval < stableDuration) < newDuration;
        }

        // PATCH api/values/5
        [HttpPatch("{id}")]
        public PimixActionResult Patch(string id, [FromBody] TDataModel value) {
            value.Id ??= Uri.UnescapeDataString(id);
            var data = Client.Get(value.Id);
            JsonConvert.PopulateObject(JsonConvert.SerializeObject(value, Defaults.JsonSerializerSettings), data,
                Defaults.JsonSerializerSettings);
            Client.Set(data);
            return RestActionResult.SuccessResult;
        }

        // POST api/values
        [HttpPost("{id}")]
        public PimixActionResult Post(string id, [FromBody] TDataModel value) {
            value.Id ??= Uri.UnescapeDataString(id);
            value.Fill();
            Client.Set(value);
            return RestActionResult.SuccessResult;
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public PimixActionResult Delete(string id) =>
            RestActionResult.FromAction(() => Client.Delete(Uri.UnescapeDataString(id)));

        // POST api/values/$refresh?id={id}
        // TODO: should be generated.
        [HttpGet("$refresh")]
        public PimixActionResult RefreshGet([FromQuery] RefreshRequest request) => Refresh(request);

        // POST api/values/$refresh?id={id}
        // TODO: should be generated.
        [HttpPost("$refresh")]
        public PimixActionResult RefreshPost([FromBody] RefreshRequest request) => Refresh(request);

        public class RefreshRequest {
            public string Id { get; set; }
        }

        // Action [HttpAction("$refresh")]
        // TODO: Should use the attribute above.
        public virtual PimixActionResult Refresh(RefreshRequest request) =>
            RestActionResult.FromAction(() => Client.Refresh(request.Id));
    }

    public class PimixActionResult : IConvertToActionResult {
        RestActionResult Result { get; set; }

        public static implicit operator PimixActionResult(RestActionResult result) {
            return new PimixActionResult {Result = result};
        }

        public IActionResult Convert() =>
            ((IConvertToActionResult) new ActionResult<RestActionResult>(Result)).Convert();
    }

    public class PimixActionResult<TValue> : IConvertToActionResult {
        RestActionResult<TValue> Result { get; set; }

        public static implicit operator PimixActionResult<TValue>(TValue value) {
            return new PimixActionResult<TValue> {Result = new RestActionResult<TValue>(value)};
        }

        public IActionResult Convert() =>
            ((IConvertToActionResult) new ActionResult<RestActionResult<TValue>>(Result)).Convert();
    }
}
