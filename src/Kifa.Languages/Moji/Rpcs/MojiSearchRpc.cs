using System;
using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;
using Newtonsoft.Json;

namespace Kifa.Languages.Moji.Rpcs;

public sealed class MojiSearchRpc : KifaJsonParameterizedRpc<MojiSearchRpc.Response> {
    #region MojiSearchRpc.Response

    public class Response {
        public Result Result { get; set; }
    }

    public class Result {
        public string OriginalSearchText { get; set; }
        public List<SearchResult> SearchResults { get; set; }
    }

    public class SearchResult {
        public string SearchText { get; set; }
        public long Count { get; set; }
        public string TarId { get; set; }
        public string Title { get; set; }
        public long Type { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string Excerpt { get; set; }
        public bool IsFree { get; set; }
    }

    #endregion

    public override string UrlPattern { get; } =
        "https://api.mojidict.com/parse/functions/search_v3";

    public override HttpMethod Method { get; } = HttpMethod.Post;

    public override bool CamelCase { get; set; } = true;

    public override string? JsonContent { get; } = JsonConvert.SerializeObject(
        new Dictionary<string, string> {
            { "searchText", "{word}" },
            { "langEnv", "zh-CN_ja" },
            { "_SessionToken", Configs.SessionToken },
            { "_ApplicationId", Configs.ApplicationId }
        });

    public MojiSearchRpc(string word) {
        parameters = new Dictionary<string, string> {
            { "word", word }
        };
    }
}
