using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Kifa.Cloud.Swisscom;
using Kifa.IO;

namespace Kifa.Api.Files;

public partial class KifaFile {
    public List<(CloudTarget target, string? destination, bool? result)> Upload(
        List<CloudTarget> targets, bool deleteSource = false, bool useCache = false,
        bool downloadLocal = false, bool skipVerify = false, bool skipRegistered = false) {
        UseCache = useCache | downloadLocal;

        try {
            // We don't really need to check source, but we need the sha256 and size to continue.
            Add(false);
            Logger.Debug($"Checked source {this}: sha256={FileInfo!.Sha256}, size={FileInfo.Size}");
        } catch (IOException ex) {
            Logger.Error(ex, $"Failed to check source {this}.");
            return targets
                .Select<CloudTarget, (CloudTarget, string?, bool?)>(target => (target, null, false))
                .ToList();
        }

        var results = targets
            .Select(target => UploadOneFile(target, deleteSource, skipVerify, skipRegistered))
            .ToList();

        CleanupFiles(
            results.Any(result => result.result == false) ? false :
            results.Any(result => result.result == null) ? null : true, deleteSource,
            downloadLocal);

        return results;
    }

    string CreateLocation(CloudTarget target)
        => FileInfo?.Sha256 == null || FileInfo?.Size == null
            ? throw new UnableToDetermineLocationException(
                $"Sha256 {FileInfo?.Sha256} or size {FileInfo?.Size} is missing.")
            : FileInfo.Locations.Keys.FirstOrDefault(l
                => new Regex(
                        $@"^{target.ServiceType.ToString().ToLower()}:[^/]+/\$/{FileInfo.Sha256}\.{target.FormatType.ToString().ToLower()}$")
                    .Match(l).Success) ?? target.ServiceType switch {
                CloudServiceType.Google =>
                    $"google:good/$/{FileInfo.Sha256}.{target.FormatType.ToString().ToLower()}",
                CloudServiceType.Swiss =>
                    // TODO: Use format specific header size.
                    $"swiss:{SwisscomStorageClient.FindAccounts(FileInfo.RealId, FileInfo.Size.Value + 0x30)}/$/{FileInfo.Sha256}.{target.FormatType.ToString().ToLower()}",
                _ => ""
            };

    (CloudTarget target, string? destination, bool? result) UploadOneFile(CloudTarget target,
        bool deleteSource, bool skipVerify, bool skipRegistered) {
        string destinationLocation;
        try {
            destinationLocation = CreateLocation(target);
        } catch (IOException ex) {
            Logger.Error(ex, $"Failed to create location to upload {this} as {target}");
            return (target, null, false);
        }

        Logger.Debug($"Will upload {this} to {destinationLocation}");

        if (!deleteSource && skipRegistered) {
            if (new KifaFile(destinationLocation).Registered) {
                Logger.Debug($"Skipped uploading of {this} to {destinationLocation} for now " +
                             "as it's supposed to be already uploaded...");
                return (target, destinationLocation, null);
            }
        }

        var destination = new KifaFile(destinationLocation);

        // Register the destination location so that it will go to the same place if retried in
        // another invocation.
        destination.Register();

        try {
            CheckDestination(destination, skipVerify);
            Logger.Debug($"Destination {destination} is already uploaded.");
            return (target, destinationLocation, true);
        } catch (FileNotFoundException) {
            // Expected not to find the destination.
        } catch (IOException ex) {
            Logger.Error(ex, $"Failed to check destination {destination}");
            return (target, destinationLocation, false);
        }

        Logger.Debug($"Copying from source {this} to destination {destination}...");
        Copy(destination);

        try {
            CheckDestination(destination, skipVerify);
            if (!skipVerify) {
                Register(true);
            }

            Logger.Debug($"Checked destination {destination}.");
            return (target, destinationLocation, true);
        } catch (IOException ex) {
            Logger.Error(ex, $"Failed to check destination {destination} after uploading.");
            return (target, destinationLocation, false);
        }
    }

    void CheckDestination(KifaFile destination, bool skipVerify) {
        // We still need to check whether file exists or not even if we skipVerify.
        if (!destination.Exists()) {
            throw new FileNotFoundException($"Unable to find destination {destination}.");
        }

        if (skipVerify) {
            return;
        }

        destination.Add();
    }

    void CleanupFiles(bool? status, bool deleteSource, bool downloadLocal) {
        if (status == false) {
            Logger.Debug("No files are removed since not all uploads are successful.");
            return;
        }

        if (!downloadLocal) {
            RemoveLocalCacheFile();
        }

        if (deleteSource && status == true) {
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
