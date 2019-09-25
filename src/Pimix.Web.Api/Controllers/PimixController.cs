using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Pimix.Service;

namespace Pimix.Web.Api.Controllers {
    [ApiController]
    public abstract class PimixController<TDataModel> : ControllerBase
        where TDataModel : DataModel {
        protected abstract PimixServiceClient<TDataModel> Client { get; }

        // GET api/values
        [HttpGet]
        public ActionResult<Dictionary<string, TDataModel>> Get()
            => new Dictionary<string, TDataModel> {
                ["/Downloads/Anime/DA01/[数码兽大冒险].[加七][Digimon_Adventure][01][GB].rmvb"] =
                    Client.Get(
                        "/Downloads/Anime/DA01/[数码兽大冒险].[加七][Digimon_Adventure][01][GB].rmvb")
            };

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<TDataModel> Get(string id) {
            id = Uri.UnescapeDataString(id);
            return Client.Get(id);
        }

        // POST api/values
        [HttpPost("{id}")]
        public void Post(string id, [FromBody] TDataModel value) {
            id = Uri.UnescapeDataString(id);
            var o = Client.Get(id);
            o.Fill();
            Client.Set(o);
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(string id) {
            id = Uri.UnescapeDataString(id);
            Client.Delete(id);
        }
    }

    public class PimixActionResult : IConvertToActionResult {
        RestActionResult Result { get; set; }

        public static implicit operator PimixActionResult(RestActionResult result) {
            return new PimixActionResult {Result = result};
        }

        public IActionResult Convert()
            => ((IConvertToActionResult)
                new ActionResult<RestActionResult>(Result)).Convert();
    }

    public class PimixActionResult<TValue> : IConvertToActionResult {
        RestActionResult<TValue> Result { get; set; }

        public static implicit operator PimixActionResult<TValue>(TValue value) {
            return new PimixActionResult<TValue> {Result = new RestActionResult<TValue>(value)};
        }

        public IActionResult Convert()
            => ((IConvertToActionResult)
                new ActionResult<RestActionResult<TValue>>(Result)).Convert();
    }
}
