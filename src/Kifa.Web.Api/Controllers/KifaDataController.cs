﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Kifa.Service;

namespace Kifa.Web.Api.Controllers {
    [ApiController]
    public abstract class KifaDataController<TDataModel, TServiceClient> : ControllerBase where TDataModel : DataModel
        where TServiceClient : KifaServiceClient<TDataModel>, new() {
        static readonly TimeSpan MinRefreshInterval = TimeSpan.FromHours(1);

        static readonly TimeSpan[] RefreshIntervals = {
            TimeSpan.FromDays(1), TimeSpan.FromDays(10), TimeSpan.FromDays(40), TimeSpan.FromDays(400)
        };

        protected readonly TServiceClient Client = new();

        // Only need to override this if the DataModel can be fill()ed but should not be.
        protected virtual bool ShouldAutoRefresh => true;

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
                var updated = value.Fill();
                if (updated != null && ShouldAutoRefresh) {
                    value.Metadata ??= new DataMetadata();
                    value.Metadata.LastRefreshed = DateTimeOffset.UtcNow;
                    value.Metadata.LastUpdated ??= value.Metadata.LastRefreshed;
                }

                Client.Set(value);
            }

            return value;
        }

        static bool NeedRefresh(TDataModel value) {
            if (RefreshIntervals == null) {
                return false;
            }

            if (value.Metadata?.LastRefreshed == null) {
                return true;
            }

            var stableDuration = value.Metadata.LastRefreshed - value.Metadata.LastUpdated;
            var newDuration = DateTimeOffset.UtcNow - value.Metadata.LastRefreshed;

            return RefreshIntervals.Reverse().FirstOrDefault(interval => interval < stableDuration)
                .Or(MinRefreshInterval) < newDuration;
        }

        // PATCH api/values/5
        [HttpPatch("{id}")]
        public PimixActionResult Patch(string id, [FromBody] TDataModel value) {
            value.Id ??= Uri.UnescapeDataString(id);
            return Client.Update(value);
        }

        // POST api/values
        [HttpPost("{id}")]
        public PimixActionResult Post(string id, [FromBody] TDataModel value) {
            value.Id ??= Uri.UnescapeDataString(id);
            value.Fill();
            return Client.Set(value);
        }

        // POST api/values/^+<TARGET>|<LINK>
        // TODO: Change to ^{target}|{link}.
        [HttpGet("^+{target}|{link}")]
        public PimixActionResult Link(string target, string link) =>
            Client.Link(Uri.UnescapeDataString(target), Uri.UnescapeDataString(link));

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public PimixActionResult Delete(string id) => Client.Delete(Uri.UnescapeDataString(id));

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
        public virtual PimixActionResult Refresh(RefreshRequest request) {
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

    public class PimixActionResult : IConvertToActionResult {
        KifaActionResult ActionResult { get; set; }

        public static implicit operator PimixActionResult(KifaActionResult actionResult) {
            return new() {ActionResult = actionResult};
        }

        public IActionResult Convert() =>
            ((IConvertToActionResult) new ActionResult<KifaActionResult>(ActionResult)).Convert();
    }

    public class PimixActionResult<TValue> : IConvertToActionResult {
        KifaActionResult<TValue> ActionResult { get; set; }

        public static implicit operator PimixActionResult<TValue>(TValue value) =>
            new() {ActionResult = new KifaActionResult<TValue>(value)};

        public IActionResult Convert() =>
            ((IConvertToActionResult) new ActionResult<KifaActionResult<TValue>>(ActionResult)).Convert();
    }
}