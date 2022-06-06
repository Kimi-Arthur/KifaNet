using System;
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
        return targets.Select(target => Upload(target, deleteSource, useCache, downloadLocal,
            skipVerify, skipRegistered)).ToList();
    }

    public (CloudTarget target, string? destination, bool? result) Upload(CloudTarget target,
        bool deleteSource = false, bool useCache = false, bool downloadLocal = false,
        bool skipVerify = false, bool skipRegistered = false) {
        UseCache = useCache | downloadLocal;
        // TODO: Better catching.
        try {
            var destinationLocation = CreateLocation(target);

            if (!deleteSource && skipRegistered) {
                if (destinationLocation != null && new KifaFile(destinationLocation).Registered) {
                    logger.Info($"Skipped uploading of {this} to {destinationLocation} for now " +
                                "as it's supposed to be already uploaded...");
                    return (target, destinationLocation, null);
                }
            }

            Register();
            if (downloadLocal) {
                // We will download it anyway.
                CacheFileToLocal();
            }

            logger.Info($"Checking source {this}...");
            try {
                Add(false);
            } catch (IOException ex) {
                if (ex is FileNotFoundException) {
                    Unregister();
                }

                logger.Error(ex, $"Source {this} is not valid.");
                return (target, destinationLocation, false);
            }

            destinationLocation ??= CreateLocation(target);
            if (destinationLocation == null) {
                logger.Error($"Upload destination cannot be determined for {target}.");
                return (target, destinationLocation, false);
            }

            var destination = new KifaFile(destinationLocation);

            if (destination.Exists()) {
                destination.Register();
                if (skipVerify) {
                    logger.Info($"Skipped verifying of {destination}.");
                    return (target, destinationLocation, true);
                }

                try {
                    destination.Add();
                    logger.Info("Already uploaded!");
                    Register(true);
                } catch (IOException ex) {
                    logger.Error(ex, $"Destination {destination} exists, but doesn't match.");
                    return (target, destinationLocation, false);
                }

                if (deleteSource) {
                    if (IsCloud) {
                        logger.Info($"Source {this} is not removed as it's in cloud.");
                    } else {
                        Delete();
                        FileInformation.Client.RemoveLocation(Id, ToString());
                        logger.Info($"Source {this} is removed since upload is successful.");
                    }
                }

                return (target, destinationLocation, true);
            }

            destination.Unregister();
            destination.Register();

            logger.Info($"Copying {this} to {destination}...");
            Copy(destination);

            if (destination.Exists()) {
                destination.Register();
                if (skipVerify) {
                    logger.Info($"Skipped verifying of {destination}.");
                    return (target, destinationLocation, true);
                }

                try {
                    logger.Info($"Checking destination {destination}...");
                    destination.Add();
                    logger.Info($"Successfully uploaded {this} to destination {destination}!");
                } catch (IOException ex) {
                    destination.Delete();
                    logger.Error(ex, $"Upload to destination {destination} failed.");
                    return (target, destinationLocation, false);
                }

                Register(true);

                if (!downloadLocal) {
                    RemoveLocalCacheFile();
                }

                if (deleteSource) {
                    if (IsCloud) {
                        logger.Info($"Source {this} is not removed as it's in cloud.");
                        return (target, destinationLocation, true);
                    }

                    Delete();
                    FileInformation.Client.RemoveLocation(Id, ToString());
                    logger.Info($"Source {this} removed since upload is successful.");
                }

                return (target, destinationLocation, true);
            }

            logger.Fatal("Destination doesn't exist unexpectedly!");
            return (target, destinationLocation, false);
        } catch (Exception ex) {
            logger.Fatal(ex, "Unexpected error");
            return (target, null, false);
        }
    }

    public string? CreateLocation(CloudTarget target)
        => FileInfo?.Sha256 == null || FileInfo?.Size == null
            ? null
            : FileInfo.Locations?.Keys.FirstOrDefault(l
                => new Regex(
                        $@"^{target.ServiceType.ToString().ToLower()}:[^/]+/\$/{FileInfo.Sha256}\.{target.FormatType.ToString().ToLower()}$")
                    .Match(l).Success) ?? target.ServiceType switch {
                CloudServiceType.Google =>
                    $"google:good/$/{FileInfo.Sha256}.{target.FormatType.ToString().ToLower()}",
                CloudServiceType.Swiss =>
                    // TODO: Use format specific header size.
                    $"swiss:{SwisscomStorageClient.FindAccounts(FileInfo.Id, FileInfo.Size.Value + 0x30)}/$/{FileInfo.Sha256}.{target.FormatType.ToString().ToLower()}",
                _ => ""
            };
}
