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

    protected override string Url => "https://api.mojidict.com/parse/functions/search_v3";

    protected override HttpMethod Method => HttpMethod.Post;

    protected override bool CamelCase => true;

    protected override string? JsonContent => JsonConvert.SerializeObject(
        new Dictionary<string, string> {
            { "searchText", "{word}" },
            { "langEnv", "zh-CN_ja" },
            { "_SessionToken", Configs.SessionToken },
            { "_ApplicationId", Configs.ApplicationId }
        });

    public MojiSearchRpc(string word) {
        Parameters = new () {
            { "word", word }
        };
    }
}
