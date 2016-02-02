using System.Runtime.Serialization;

namespace Pimix.Service
{
    public enum ActionStatusCode
    {
        [EnumMember(Value = "ok")]
        OK,
        [EnumMember(Value = "bad_request")]
        BadRequest,
        [EnumMember(Value = "error")]
        Error
    }
}
