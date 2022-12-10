using System;
using System.Collections.Generic;
using System.Net.Http;
using Kifa.Service;
using Newtonsoft.Json;
using NLog;

namespace Kifa.Cloud.Swisscom;

public class SwisscomAccountQuota : DataModel {
    public const string ModelId = "accounts/swisscom_quotas";

    static SwisscomAccountQuotaServiceClient? client;

    public static SwisscomAccountQuotaServiceClient Client
        => client ??= new SwisscomAccountQuotaRestServiceClient();

    public static SwisscomAccountServiceClient AccountClient { get; set; } =
        new SwisscomAccountRestServiceClient();

    public long TotalQuota { get; set; }
    public long UsedQuota { get; set; }

    [JsonIgnore]
    public long LeftQuota => TotalQuota - Math.Max(ExpectedQuota, UsedQuota);

    // This value will be filled when reserved.
    // When it is the same as UsedQuota, it can be safely discarded or ignored.
    public long ExpectedQuota { get; set; }

    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    readonly HttpClient httpClient = new();

    public override bool FillByDefault => true;

    public override DateTimeOffset? Fill() {
        if (AccountClient.Get(Id) == null) {
            throw new UnableToFillException($"Account {Id} is missing.");
        }

        if (UpdateQuota().Status == KifaActionStatus.OK) {
            return Date.Zero;
        }

        Logger.Info("Access token expired.");

        var result = UpdateQuota();
        if (result.Status != KifaActionStatus.OK) {
            Logger.Warn($"Failed to get quota: {result}.");
        }

        // Quota always needs to be refreshed as it may change any time.
        return Date.Zero;
    }

    KifaActionResult UpdateQuota()
        => KifaActionResult.FromAction(() => {
            var account = AccountClient.Get(Id);
            if (account?.AccessToken == null) {
                return new KifaActionResult {
                    Status = KifaActionStatus.Error,
                    Message = $"Unable to get account {Id} unexpectedly."
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
