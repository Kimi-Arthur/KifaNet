using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;
using Newtonsoft.Json;

namespace Kifa.Memrise.Api;

public sealed class AddWordRpc : KifaJsonParameterizedRpc<AddWordResponse> {
    protected override string Url => "https://app.memrise.com/ajax/thing/add/";

    protected override HttpMethod Method => HttpMethod.Post;

    protected override Dictionary<string, string> Headers
        => new() {
            { "referer", "{referer}" }
        };

    protected override List<KeyValuePair<string, string>> FormContent
        => new() {
            new KeyValuePair<string, string>("columns", "{data}"),
            new KeyValuePair<string, string>("pool_id", "{databaseId}")
        };

    public AddWordRpc(string databaseId, string referer, Dictionary<string, string> data) {
        Parameters = new () {
            { "databaseId", databaseId },
            { "referer", referer },
            { "data", JsonConvert.SerializeObject(data) }
        };
    }
}

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
