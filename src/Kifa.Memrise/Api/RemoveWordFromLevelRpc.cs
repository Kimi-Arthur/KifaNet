using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Memrise.Api {
    public class RemoveWordFromLevelRpc : JsonRpc<RemoveWordFromLevelRpc.RemoveWordFromLevelResponse> {
        public class RemoveWordFromLevelResponse {
            public bool? Success { get; set; }
        }

        public override HttpMethod Method { get; } = HttpMethod.Post;

        public override Dictionary<string, string> Headers { get; } = new() {{"referer", "{referer}"}};

        public override string UrlPattern { get; } = "https://app.memrise.com/ajax/level/thing_remove/";

        public override List<KeyValuePair<string, string>> FormContent { get; set; } =
            new() {new("level_id", "{level_id}"), new("thing_id", "{thing_id}")};

        public RemoveWordFromLevelResponse Call(string referer, string levelId, string thingId) {
            return Call(new Dictionary<string, string> {
                {"referer", referer}, {"level_id", levelId}, {"thing_id", thingId}
            });
        }
    }
}