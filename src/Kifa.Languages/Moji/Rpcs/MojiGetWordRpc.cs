using System;
using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;
using Newtonsoft.Json;

namespace Kifa.Languages.Moji.Rpcs;

public sealed class MojiGetWordRpc : KifaJsonParameterizedRpc<MojiGetWordRpc.Response> {
    #region MojiGetWordRpc.Response

    public class Response {
        public ResponseResult Result { get; set; }
    }

    public class ResponseResult {
        public List<ResultElement> Result { get; set; }
        public long Code { get; set; }
    }

    public class ResultElement {
        public Word Word { get; set; }
        public List<Detail> Details { get; set; }
        public List<Detail> Subdetails { get; set; }
        public List<Example> Examples { get; set; }
    }

    public class Detail {
        public string Title { get; set; }
        public long Index { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public string WordId { get; set; }
        public string UpdatedBy { get; set; }
        public string ObjectId { get; set; }
        public string DetailsId { get; set; }
    }

    public class Example {
        public string Title { get; set; }
        public long Index { get; set; }
        public string Trans { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public string WordId { get; set; }
        public string SubdetailsId { get; set; }
        public string UpdatedBy { get; set; }
        public bool IsFree { get; set; }
        public long Quality { get; set; }
        public bool IsChecked { get; set; }
        public long ViewedNum { get; set; }
        public string ObjectId { get; set; }
        public string NotationTitle { get; set; }
    }

    public class Word {
        public string Excerpt { get; set; }
        public string Spell { get; set; }
        public string Accent { get; set; }
        public string Pron { get; set; }
        public string Romaji { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
        public string Tags { get; set; }
        public long OutSharedNum { get; set; }
        public long VTag { get; set; }
        public bool IsFree { get; set; }
        public long Quality { get; set; }
        public long ViewedNum { get; set; }
        public string ObjectId { get; set; }
    }

    #endregion

    protected override string Url
        => "https://api.mojidict.com/parse/functions/nlt-fetchManyLatestWords";

    protected override HttpMethod Method => HttpMethod.Post;

    protected override string? JsonContent => JsonConvert.SerializeObject(
        new Dictionary<string, object> {
            {
                "itemsJson", new List<Dictionary<string, string>> {
                    new() {
                        { "objectId", "{word_id}" }
                    }
                }
            },
            { "skipAccessories", false },
            { "_SessionToken", Configs.SessionToken },
            { "_ApplicationId", Configs.ApplicationId }
        });

    protected override bool CamelCase => true;

    public MojiGetWordRpc(string wordId) {
        Parameters = new () {
            { "word_id", wordId }
        };
    }
}
