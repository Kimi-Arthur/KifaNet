using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson.Serialization.Attributes;
using Pimix.IO;
using Pimix.Service;

namespace Pimix.Web.Api.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase {
        PimixServiceClient client = new PimixServiceJsonClient();
        
        // GET api/values
        [HttpGet]
        public Microsoft.AspNetCore.Mvc.ActionResult<Dictionary<string, FileInformation>> Get() {
            return new Dictionary<string, FileInformation> {
                ["/Downloads/Anime/DA01/[数码兽大冒险].[加七][Digimon_Adventure][01][GB].rmvb"] =
                    client.Get<FileInformation>(
                        "/Downloads/Anime/DA01/[数码兽大冒险].[加七][Digimon_Adventure][01][GB].rmvb")
            };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public Microsoft.AspNetCore.Mvc.ActionResult<FileInformation> Get(string id) {
            id = Uri.UnescapeDataString(id);
            return client.Get<FileInformation>(id);
        }

        // POST api/values
        [HttpPost("{id}")]
        public void Post(string id, [FromBody] FileInformation value) {
            id = Uri.UnescapeDataString(id);
            client.Get<FileInformation>(id);
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(string id) {
            id = Uri.UnescapeDataString(id);
            client.Delete<FileInformation>(id);
        }
    }

    public class SoccerTeam {
        public string Id { get; set; }

        [BsonElement("short_name")]
        public string ShortName { get; set; }

        [BsonElement("full_name")]
        public string FullName { get; set; }
    }
}
