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
                var sourceCheckResult = Add();

                if (sourceCheckResult != FileProperties.None) {
                    logger.Error(
                        $"Source is wrong! The following fields differ: {sourceCheckResult}");
                    return (target, destinationLocation, false);
                }
            } catch (FileNotFoundException ex) {
                Unregister();
                logger.Error(ex, "Source file not found.");
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

                var destinationCheckResult = destination.Add();

                if (destinationCheckResult == FileProperties.None) {
                    logger.Info("Already uploaded!");
                    Register(true);

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

                logger.Error("Destination exists, but doesn't match.");
                return (target, destinationLocation, false);
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

                logger.Info("Checking {0}...", destination);
                var destinationCheckResult = destination.Add();
                if (destinationCheckResult == FileProperties.None) {
                    logger.Info($"Successfully uploaded {this} to {destination}!");
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

                destination.Delete();
                logger.Error("Upload failed! The following fields differ (removed): {0}",
                    destinationCheckResult);
                return (target, destinationLocation, false);
            }

            logger.Fatal("Destination doesn't exist unexpectedly!");
            return (target, destinationLocation, false);
        } catch (Exception ex) {
            logger.Fatal(ex, "Unexpected error");
            return (target, null, false);
        }
    }

    public string CreateLocation(CloudTarget target)
        => FileInfo?.Sha256 == null || FileInfo?.Size == null
            ? null
            : FileInfo.Locations.Keys.FirstOrDefault(l
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
