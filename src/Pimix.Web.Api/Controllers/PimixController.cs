using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Pimix.Service;

namespace Pimix.Web.Api.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public abstract class PimixController<TDataModel> : ControllerBase {
        protected static PimixServiceClient<TDataModel> client = new PimixServiceJsonClient<TDataModel>();

        // GET api/values
        [HttpGet]
        public ActionResult<Dictionary<string, TDataModel>> Get() {
            return new Dictionary<string, TDataModel> {
                ["/Downloads/Anime/DA01/[数码兽大冒险].[加七][Digimon_Adventure][01][GB].rmvb"] =
                    client.Get("/Downloads/Anime/DA01/[数码兽大冒险].[加七][Digimon_Adventure][01][GB].rmvb")
            };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<TDataModel> Get(string id) {
            id = Uri.UnescapeDataString(id);
            return client.Get(id);
        }

        // POST api/values
        [HttpPost("{id}")]
        public void Post(string id, [FromBody] TDataModel value) {
            id = Uri.UnescapeDataString(id);
            client.Get(id);
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(string id) {
            id = Uri.UnescapeDataString(id);
            client.Delete(id);
        }
    }
}
