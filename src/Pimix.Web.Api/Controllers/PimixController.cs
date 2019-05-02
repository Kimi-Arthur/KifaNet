using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Pimix.Service;

namespace Pimix.Web.Api.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public abstract class PimixController<TEntity> : ControllerBase {
        protected PimixServiceClient client = new PimixServiceRestClient();

        // GET api/values
        [HttpGet]
        public ActionResult<Dictionary<string, TEntity>> Get() {
            return new Dictionary<string, TEntity> {
                ["/Downloads/Anime/DA01/[数码兽大冒险].[加七][Digimon_Adventure][01][GB].rmvb"] =
                    client.Get<TEntity>("/Downloads/Anime/DA01/[数码兽大冒险].[加七][Digimon_Adventure][01][GB].rmvb")
            };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<TEntity> Get(string id) {
            id = Uri.UnescapeDataString(id);
            return client.Get<TEntity>(id);
        }

        // POST api/values
        [HttpPost("{id}")]
        public void Post(string id, [FromBody] TEntity value) {
            id = Uri.UnescapeDataString(id);
            client.Get<TEntity>(id);
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(string id) {
            id = Uri.UnescapeDataString(id);
            client.Delete<TEntity>(id);
        }
    }
}
