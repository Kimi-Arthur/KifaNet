using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Pimix.Web.Api.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase {
        readonly IMongoCollection<SoccerTeam> data;

        public ValuesController() {
            var client = new MongoClient("mongodb://www.pimix.tk:27017");
            var db = client.GetDatabase("soccer");
            data = db.GetCollection<SoccerTeam>("teams");
        }

        // GET api/values
        [HttpGet]
        public ActionResult<Dictionary<string, SoccerTeam>> Get() {
            return data.Find(x => true).ToList().ToDictionary(x => x.Id, x => x);
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<SoccerTeam> Get(string id) {
            return data.Find(x => x.Id == id).Single();
        }

        // POST api/values
        [HttpPost("{id}")]
        public void Post(string id, [FromBody] SoccerTeam value) {
            if (value.Id == null) {
                value.Id = id;
            }

            data.InsertOne(value);
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(string id) {
            data.DeleteOne(x => x.Id == id);
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
