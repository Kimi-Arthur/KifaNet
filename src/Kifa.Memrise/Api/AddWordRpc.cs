using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;
using Newtonsoft.Json;

namespace Kifa.Memrise.Api {
    public class AddWordRpc : JsonRpc<AddWordRpc.AddWordResponse> {
        public class AddWordResponse {
            public bool? Success { get; set; }
            public Thing Thing { get; set; }
            public string RenderedThing { get; set; }
        }

        public class Thing {
            public long Id { get; set; }
            public long PoolId { get; set; }
            public Dictionary<string, Column> Columns { get; set; }
            public Attributes Attributes { get; set; }
        }

        public class Attributes {
        }

        public class Column {
            public List<object> Alts { get; set; }
            public string Val { get; set; }
            public List<object> Choices { get; set; }
            public Distractors Distractors { get; set; }
            public string Kind { get; set; }
            public List<string> Accepted { get; set; }
            public Attributes TypingCorrects { get; set; }
        }

        public class Distractors {
            public List<object> Typing { get; set; }
            public List<object> Tapping { get; set; }
            public List<object> MultipleChoice { get; set; }
            public List<object> Audio { get; set; }
            public List<object> Default { get; set; }
        }

        public override HttpMethod Method { get; } = HttpMethod.Post;

        public override Dictionary<string, string> Headers { get; } = new() {{"referer", "{referer}"}};

        public override string UrlPattern { get; } = "https://app.memrise.com/ajax/thing/add/";

        public override List<KeyValuePair<string, string>> FormUrlEncodedContent { get; set; } =
            new() {new("columns", "{data}"), new("pool_id", "{databaseId}")};

        public AddWordResponse Call(string databaseId, string referer, Dictionary<string, string> data) {
            return Call(new Dictionary<string, string> {
                {"databaseId", databaseId}, {"referer", referer}, {"data", JsonConvert.SerializeObject(data)}
            });
        }
    }
}
