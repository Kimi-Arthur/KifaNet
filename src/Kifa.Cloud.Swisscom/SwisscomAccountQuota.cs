using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Kifa.IO;
using Kifa.Service;
using Newtonsoft.Json;

namespace Kifa.Cloud.Swisscom;

public class SwisscomAccountQuota : DataModel {
    public const string ModelId = "swisscom/quotas";

    static SwisscomAccountQuotaServiceClient? client;

    public static SwisscomAccountQuotaServiceClient Client
        => client ??= new SwisscomAccountQuotaRestServiceClient();

    public static List<StorageMapping> StorageMappings { get; set; }

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

    // TODO(#2): Should implement in server side.
    public static string FindAccounts(string path, long length) {
        var prefixes = StorageMappings.First(mapping => path.StartsWith(mapping.Pattern))
            .AccountPrefixes;
        var selectedAccounts = new List<string>();
        for (var i = 0L; i < length; i += SwisscomStorageClient.ShardSize) {
            selectedAccounts.Add(FindAccount(prefixes,
                Math.Min(SwisscomStorageClient.ShardSize, length - i)));
        }

        return string.Join("+", selectedAccounts);
    }

    public static string FindAccount(List<string> prefixes, long length) {
        var lookForAligned = IsAligned(length);
        var account = Client.List().Values.Where(account
                => prefixes.Any(prefix => account.Id.StartsWith(prefix)) &&
                   IsAligned(account.LeftQuota) == lookForAligned && account.LeftQuota >= length)
            .MinBy(a => a.LeftQuota);

        if (account == null) {
            throw new InsufficientStorageException(
                $"Unable to find a proper account to hold {length}B data.");
        }

        // We will assume the quota we get here is up to date.
        var accountId = account.Id;
        account = Client.Get(account.Id);

        if (account == null) {
            throw new InsufficientStorageException(
                $"Unexpectedly, failed to get the account {accountId}.");
        }

        if (account.LeftQuota < length) {
            throw new InsufficientStorageException(
                $"Unexpectedly, the account {account.Id} doesn't have enough quota {account.LeftQuota} < {length}.");
        }

        var result = Client.ReserveQuota(account.Id, length);
        if (result.Status != KifaActionStatus.OK) {
            throw new InsufficientStorageException(
                $"Failed to reserve quota {length}B in {account.Id}.");
        }

        return account.Id;
    }

    static bool IsAligned(long length) => length % SwisscomStorageClient.ShardSize == 0;

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

public class StorageMapping {
    #region public late string Pattern { get; set; }

    string? pattern;

    public string Pattern {
        get => Late.Get(pattern);
        set => Late.Set(ref pattern, value);
    }

    #endregion

    public List<string> AccountPrefixes { get; set; } = new();
}
