using System;
using System.Collections.Generic;
using System.Net.Http;
using Kifa.Service;
using Newtonsoft.Json;
using NLog;

namespace Kifa.Cloud.Swisscom;

public class SwisscomAccountQuota : DataModel<SwisscomAccountQuota> {
    public const string ModelId = "accounts/swisscom_quotas";

    static SwisscomAccountQuotaServiceClient client;

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

    static readonly Logger logger = LogManager.GetCurrentClassLogger();

    readonly HttpClient httpClient = new();

    public override bool FillByDefault => true;

    public override DateTimeOffset? Fill() {
        if (UpdateQuota().Status == KifaActionStatus.OK) {
            return Date.Zero;
        }

        logger.Info("Access token expired.");

        var result = UpdateQuota();
        if (result.Status != KifaActionStatus.OK) {
            logger.Warn($"Failed to get quota: {result}.");
        }

        // Quota always needs to be refreshed as it may change any time.
        return Date.Zero;
    }

    KifaActionResult UpdateQuota()
        => KifaActionResult.FromAction(() => {
            using var response = httpClient.SendWithRetry(()
                => SwisscomStorageClient.APIList.Quota.GetRequest(new Dictionary<string, string> {
                    ["access_token"] = AccountClient.Get(Id!)!.AccessToken
                }));
            var data = response.GetJToken();
            UsedQuota = data.Value<long>("TotalBytes");
            TotalQuota = data.Value<long>("StorageLimit");
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
