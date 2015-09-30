using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Pimix.Service
{
    public class ActionStatus
    {
        [JsonProperty("status")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ActionStatusCode StatusCode { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public enum ActionStatusCode
    {
        OK,
        BadRequest,
        Error,
        // Aliases
        ok = OK,
        bad_request = BadRequest,
        error = Error
    };
}
