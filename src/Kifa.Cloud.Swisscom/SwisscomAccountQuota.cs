using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Kifa.IO;
using Kifa.Service;
using Newtonsoft.Json;
using NLog;

namespace Kifa.Cloud.Swisscom;

public class SwisscomAccountQuota : DataModel, WithModelId {
    public static string ModelId => "swisscom/quotas";

    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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

    public Dictionary<string, long> Reservations { get; set; } = new();

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
    public static string FindAccounts(string logicalPath, string actualPath, long length) {
        var prefixes = StorageMappings.First(mapping => logicalPath.StartsWith(mapping.Pattern))
            .AccountPrefixes;
        var selectedAccounts = new List<string>();
        for (var i = 0L; i < length; i += SwisscomStorageClient.ShardSize) {
            // The rule to construct the actual path is implicitly related to
            // ShardedStorageClient.GetShards.
            selectedAccounts.Add(FindAccount(prefixes,
                $"{actualPath}.{i / SwisscomStorageClient.ShardSize}",
                Math.Min(SwisscomStorageClient.ShardSize, length - i)));
        }

        return string.Join("+", selectedAccounts);
    }

    public static string FindAccount(List<string> prefixes, string path, long length) {
        var accounts = Client.List().Values.ToList();
        var existingReservation = FindExistingReservation(accounts, path, length);
        if (existingReservation != null) {
            return existingReservation;
        }

        var account =
            GetAccount(
                accounts.Where(account
                    => prefixes.Any(prefix => account.Id.StartsWith(prefix)) &&
                       account.LeftQuota >= length).ToList(), length);

        if (account == null) {
            throw new InsufficientStorageException(
                $"Unable to find a proper account to hold {length} bytes.");
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

        var result = Client.ReserveQuota(account.Id, path, length);
        if (result.Status != KifaActionStatus.OK) {
            throw new InsufficientStorageException(
                $"Failed to reserve quota {length} bytes in {account.Id}.");
        }

        return account.Id;
    }

    static SwisscomAccountQuota? GetAccount(List<SwisscomAccountQuota> accounts, long length) {
        var lookForAligned = IsAligned(length);
        var foundAccount = accounts.Where(account => IsAligned(account.LeftQuota) == lookForAligned)
            .MinBy(a => a.LeftQuota);
        if (foundAccount != null) {
            return foundAccount;
        }

        if (lookForAligned) {
            // There are no aligned accounts, maybe there are unaligned ones.
            // Let's find the smallest possible to fit as we treat this as a normal unaligned piece.
            return accounts.MinBy(account => account.LeftQuota);
        }

        // For an unaligned piece, we may want to choose a bigger account
        // so that number of "polluted" accounts is minimized.
        return accounts.MaxBy(account => account.LeftQuota);
    }

    static string? FindExistingReservation(List<SwisscomAccountQuota> accounts, string path,
        long length) {
        var account = accounts.FirstOrDefault(account => account.Reservations.ContainsKey(path));
        if (account == null) {
            return null;
        }

        var reservation = account.Reservations[path];
        if (reservation != length) {
            throw new Exception(
                $"Unexpected length mismatch for reservation of {path} in {account.Id}. " +
                $"Expected {length}, found {reservation}.");
        }

        return account.Id;
    }

    static bool IsAligned(long length) => length % SwisscomStorageClient.ShardSize == 0;

    void ReconcileQuota() {
        if (UsedQuota == ExpectedQuota) {
            ExpectedQuota = 0;
        }

        var storageClient = SwisscomStorageClient.Create(Id);
        var fulfilledReservations = new List<string>();
        foreach (var reservation in Reservations) {
            var length = storageClient.Length(reservation.Key);
            if (length == reservation.Value) {
                Logger.Debug(
                    $"Reservation for {reservation.Key} ({reservation.Value}) is done in account {Id}.");
                fulfilledReservations.Add(reservation.Key);
            } else if (length > 0) {
                throw new Exception(
                    $"File size of {reservation.Key} ({length}) is unexpected ({reservation.Value}).");
            }
        }

        foreach (var fulfilled in fulfilledReservations) {
            Reservations.Remove(fulfilled);
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
    KifaActionResult ReserveQuota(string id, string path, long length);
    KifaActionResult ClearReserve(string id);
}

public class SwisscomAccountQuotaRestServiceClient : KifaServiceRestClient<SwisscomAccountQuota>,
    SwisscomAccountQuotaServiceClient {
    public List<SwisscomAccountQuota> GetTopAccounts()
        => Call<List<SwisscomAccountQuota>>("get_top_accounts");

    public KifaActionResult ReserveQuota(string id, string path, long length)
        => Call("reserve_quota", new Dictionary<string, object> {
            { "id", id },
            { "path", path },
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
