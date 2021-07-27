using Newtonsoft.Json;

namespace Kifa.Web.Api {
    public abstract class KifaRequest {
        public override string ToString() => JsonConvert.SerializeObject(this, Defaults.PrettyJsonSerializerSettings);
    }
}
