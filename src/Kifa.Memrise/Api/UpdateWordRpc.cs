using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Memrise.Api {
    public class UpdateWordRpc : JsonRpc<UpdateWordRpc.UpdateWordResponse> {
        public class UpdateWordResponse {
            public bool? Success { get; set; }
        }

        public override HttpMethod Method { get; } = HttpMethod.Post;

        public override Dictionary<string, string> Headers { get; } = new() {{"referer", "{referer}"}};

        public override string UrlPattern { get; } = "https://app.memrise.com/ajax/thing/cell/update/";

        public override List<KeyValuePair<string, string>> FormContent { get; set; } = new() {
            new("thing_id", "{thing_id}"),
            new("cell_id", "{cell_id}"),
            new("cell_type", "column"),
            new("new_val", "{value}")
        };

        public UpdateWordResponse Call(string referer, string thingId, string cellId, string value) {
            return Call(new Dictionary<string, string> {
                {"referer", referer}, {"thing_id", thingId}, {"cell_id", cellId}, {"value", value}
            });
        }
    }
}
