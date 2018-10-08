using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Pimix.Web.Api.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase {
        HttpClient client;
        HttpClient Client => client = client ?? new HttpClient();
        ILogger _logger;

        public ValuesController(ILogger<ValuesController> logger) {
            _logger = logger;
        }

        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get() {
            return new string[] {"value1", "value2"};
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id) {
            var request =
                new HttpRequestMessage(HttpMethod.Post,
                    "https://applens.azurewebsites.net/api/invoke");

            foreach (var header in HttpContext.Request.Headers) {
                if (header.Key == "Authorization" || header.Key.StartsWith("x-ms-")) {
                    request.Headers.Add(header.Key, header.Value.ToString());
                }
            }
            
            request.Content = new StringContent("");
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            return Client.SendAsync(request).Result.Content.ReadAsStringAsync().Result;
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value) {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value) {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id) {
        }
    }
}
