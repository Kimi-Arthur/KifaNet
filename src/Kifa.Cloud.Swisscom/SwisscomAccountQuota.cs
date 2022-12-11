using System;
using System.Collections.Generic;
using System.Net.Http;
using Kifa.Service;
using Newtonsoft.Json;

namespace Kifa.Cloud.Swisscom;

public class SwisscomAccountQuota : DataModel {
    public const string ModelId = "swisscom/quotas";

    static SwisscomAccountQuotaServiceClient? client;

    public static SwisscomAccountQuotaServiceClient Client
        => client ??= new SwisscomAccountQuotaRestServiceClient();

    public long TotalQuota { get; set; }
    public long UsedQuota { get; set; }

    [JsonIgnore]
    public long LeftQuota => TotalQuota - Math.Max(ExpectedQuota, UsedQuota);

    // This value will be filled when reserved.
    // When it is the same as UsedQuota, it can be safely discarded or ignored.
    public long ExpectedQuota { get; set; }

    readonly HttpClient httpClient = new();

    public override bool FillByDefault => true;

    public override DateTimeOffset? Fill() {
        var account = SwisscomAccount.Client.Get(Id);
        if (account == null) {
            throw new UnableToFillException($"Account {Id} is missing.");
        }

        var result = UpdateQuota(account);
        if (result.Status != KifaActionStatus.OK) {
            throw new UnableToFillException(result.Message!);
        }

        ReconcileQuota();
        return Date.Zero;
    }

    void ReconcileQuota() {
        if (UsedQuota == ExpectedQuota) {
            ExpectedQuota = 0;
        }
    }

    KifaActionResult UpdateQuota(SwisscomAccount account)
        => KifaActionResult.FromAction(() => {
            if (account.AccessToken == null) {
                return new KifaActionResult {
                    Status = KifaActionStatus.Error,
                    Message = $"Unable to get access token for account {Id} unexpectedly."
                };
            }

            var response = httpClient.FetchJToken(()
                => SwisscomStorageClient.APIList.Quota.GetRequest(new Dictionary<string, string> {
                    ["access_token"] = account.AccessToken
                }));
            UsedQuota = response.Value<long>("TotalBytes");
            TotalQuota = response.Value<long>("StorageLimit");
            return KifaActionResult.Success;
        });
}

public interface SwisscomAccountQuotaServiceClient : KifaServiceClient<SwisscomAccountQuota> {
    List<SwisscomAccountQuota> GetTopAccounts();
    KifaActionResult ReserveQuota(string id, long length);
    KifaActionResult ClearReserve(string id);
}

public class SwisscomAccountQuotaRestServiceClient : KifaServiceRestClient<SwisscomAccountQuota>,
    SwisscomAccountQuotaServiceClient {
    public List<SwisscomAccountQuota> GetTopAccounts()
        => Call<List<SwisscomAccountQuota>>("get_top_accounts");

    public KifaActionResult ReserveQuota(string id, long length)
        => Call("reserve_quota", new Dictionary<string, object> {
            { "id", id },
            { "length", length }
        });

    public KifaActionResult ClearReserve(string id)
        => Call("clear_all_reserves", new Dictionary<string, object> {
            { "id", id }
        });
}
