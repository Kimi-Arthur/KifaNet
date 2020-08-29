using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Newtonsoft.Json;
using Pimix.Service;

namespace Pimix.Web.Api.Controllers {
    [ApiController]
    public abstract class PimixController<TDataModel> : ControllerBase where TDataModel : DataModel {
        protected abstract PimixServiceClient<TDataModel> Client { get; }

        // GET api/values
        [HttpGet]
        public ActionResult<SortedDictionary<string, TDataModel>> List() => Client.List();

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<TDataModel> Get(string id) {
            id = Uri.UnescapeDataString(id);
            if (id.StartsWith("$")) {
                return new NotFoundResult();
            }

            return Client.Get(id);
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
        public PimixActionResult Delete(string id) {
            id = Uri.UnescapeDataString(id);
            Client.Delete(id);
            return RestActionResult.SuccessResult;
        }

        // GET api/values/$refresh?id={id}
        [HttpGet("$refresh")]
        public PimixActionResult Refresh(string id) {
            RefreshPost(new RefreshRequest {Id = Uri.UnescapeDataString(id)});
            return RestActionResult.SuccessResult;
        }

        // POST api/values/$refresh?id={id}
        [HttpPost("$refresh")]
        public PimixActionResult RefreshPost([FromBody] RefreshRequest request) {
            Client.Refresh(request.Id);
            return RestActionResult.SuccessResult;
        }
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

    public class RefreshRequest {
        public string Id { get; set; }
    }
}
