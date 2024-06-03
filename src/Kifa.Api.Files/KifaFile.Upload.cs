using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Kifa.Cloud.Google;
using Kifa.Cloud.Swisscom;
using Kifa.Cloud.Telegram;
using Kifa.IO;
using Kifa.Service;

namespace Kifa.Api.Files;

public partial class KifaFile {
    #region public late static string TelegramCell { get; set; }

    static string? telegramCell;

    public static string TelegramCell {
        get => Late.Get(telegramCell);
        set => Late.Set(ref telegramCell, value);
    }

    #endregion

    public KifaActionResult Upload(List<CloudTarget> targets, bool deleteSource = false,
        bool useCache = false, bool downloadLocal = false, bool skipVerify = false,
        bool skipRegistered = false) {
        UseCache = useCache | downloadLocal;

        try {
            // We don't really need to check source, but we need the sha256 and size to continue.
            Add(false);
            Logger.Debug($"Checked source {this}: sha256={FileInfo!.Sha256}, size={FileInfo.Size}");
        } catch (IOException ex) {
            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = $"Failed to check source {this}: {ex}"
            };
        }

        var result = new KifaBatchActionResult();
        result.AddRange(targets.Select(target => (target.ToString(),
            UploadOneFile(target, deleteSource, skipVerify, skipRegistered))));

        if (result.IsAcceptable) {
            CleanupFiles(deleteSource, downloadLocal);
        }

        return result;
    }

    string CreateLocation(CloudTarget target) {
        if (FileInfo?.Sha256 == null || FileInfo?.Size == null) {
            throw new UnableToDetermineLocationException(
                $"Sha256 {FileInfo?.Sha256} or size {FileInfo?.Size} is missing.");
        }

        var encodedSize = Length + target.FormatType.HeaderSize;
        Logger.Debug($"Supposed upload size is {encodedSize}.");

        return FileInfo.Locations.Keys.FirstOrDefault(l
            => new Regex(
                    $@"^{target.ServiceType.ToString().ToLower()}:[^/]+/\$/{FileInfo.Sha256}\.{target.FormatType}$")
                .Match(l).Success) ?? target.ServiceType switch {
            CloudServiceType.Google => GoogleDriveStorageClient.CreateLocation(FileInfo,
                target.FormatType),
            CloudServiceType.Swiss =>
                $"swiss:{SwisscomStorageClient.FindAccounts(FileInfo.RealId, $"/$/{FileInfo.Sha256}.{target.FormatType}", encodedSize)}/$/{FileInfo.Sha256}.{target.FormatType}",
            CloudServiceType.Tele => $"{TelegramStorageClient.CreateLocation(FileInfo, TelegramCell,
                encodedSize)}.{target.FormatType}",
            _ => ""
        };
    }

    KifaActionResult UploadOneFile(CloudTarget target, bool deleteSource, bool skipVerify,
        bool skipRegistered) {
        string destinationLocation;
        try {
            destinationLocation = CreateLocation(target);
        } catch (IOException ex) {
            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = $"Failed to create location to upload {this} to {target}: {ex}"
            };
        }

        Logger.Debug($"Will upload {this} to {destinationLocation}.");

        if (!deleteSource && skipRegistered) {
            if (new KifaFile(destinationLocation).Registered) {
                return new KifaActionResult {
                    Status = KifaActionStatus.Pending,
                    Message = $"Skipped uploading of {this} to {destinationLocation} for now " +
                              "as it's supposed to be already uploaded..."
                };
            }
        }

        var destination = new KifaFile(destinationLocation);

        try {
            CheckDestination(destination, skipVerify);
            return new KifaActionResult {
                Status = KifaActionStatus.OK,
                Message = $"Destination {destination} is already uploaded."
            };
        } catch (FileNotFoundException) {
            // Expected not to find the destination.
        } catch (IOException ex) {
            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = $"Failed to check destination {destination}: {ex}"
            };
        }

        // Register the destination location so that it will go to the same place if retried in
        // another invocation. Unregister first as the existing entry is probably phantom as the
        // last step isn't enough to convince otherwise.
        destination.Unregister();
        destination.Register();

        Logger.Debug($"Copying from source {this} to destination {destination}...");
        Copy(destination);

        try {
            CheckDestination(destination, skipVerify);
            return new KifaActionResult {
                Status = KifaActionStatus.OK,
                Message = $"Uploaded to destination {destination}."
            };
        } catch (IOException ex) {
            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = $"Failed to check destination {destination} after uploading: {ex}"
            };
        }
    }

    static void CheckDestination(KifaFile destination, bool skipVerify) {
        // We still need to check whether file exists or not even if we skipVerify.
        if (!destination.Exists()) {
            throw new FileNotFoundException($"Unable to find destination {destination}.");
        }

        if (skipVerify) {
            return;
        }

        destination.Add();
    }

    void CleanupFiles(bool deleteSource, bool downloadLocal) {
        if (!downloadLocal) {
            RemoveLocalCacheFile();
        }

        if (deleteSource) {
            if (IsCloud) {
                Logger.Warn($"Source {this} is not removed as it's in cloud.");
            } else {
                Delete();
                FileInformation.Client.RemoveLocation(Id, ToString());
                Logger.Debug($"Source {this} is removed since upload is successful.");
            }
        }
    }
}
