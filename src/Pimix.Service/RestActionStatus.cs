using System.Runtime.Serialization;

namespace Pimix.Service {
    public enum RestActionStatus {
        [EnumMember(Value = "ok")]
        OK,

        [EnumMember(Value = "bad_request")]
        BadRequest,

        [EnumMember(Value = "error")]
        Error
    }
}
