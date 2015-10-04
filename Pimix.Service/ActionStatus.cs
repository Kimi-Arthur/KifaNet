using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Pimix.Service
{
    public class ActionStatus<ResponseType>
    {
        [JsonProperty("status")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ActionStatusCode StatusCode { get; set; }

        [JsonProperty("message")]
        public ResponseType Message { get; set; }

        public static implicit operator ActionStatus(ActionStatus<ResponseType> value)
            => new ActionStatus
            {
                StatusCode = value.StatusCode,
                Message = value.Message.ToString()
            };
    }

    public class ActionStatus : ActionStatus<string>
    {
        public override string ToString()
            => $"status: {StatusCode}, message: {Message}";
    }

    public enum ActionStatusCode
    {
        [EnumMember(Value = "ok")]
        OK,
        [EnumMember(Value = "bad_request")]
        BadRequest,
        [EnumMember(Value = "error")]
        Error
    };
}
