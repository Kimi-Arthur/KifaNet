using CommandLine;
using Kifa.Jobs;
using Kifa.Service;
using Kifa.YouTube;
using NLog;

namespace Kifa.Tools.YoutubeUtil.Commands;

[Verb("name", HelpText = "Add names, aliases, or link channel IDs/handles for a YouTube uploader.")]
public class AddUploaderNameCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Uploader ID, handle, or name.")]
    public string UploaderId { get; set; } = "";

    [Value(1, Required = false, HelpText = "Name(s), alias(es), or channel link(s) (e.g. @handle, UC...) to add.")]
    public IEnumerable<string> Names { get; set; } = [];

    [Option('c', "canonical", HelpText = "Set the (first) name as the canonical Name instead of an alias.")]
    public bool AsCanonical { get; set; } = false;

    [Option('r', "refresh", HelpText = "Force refresh server data before adding.")]
    public bool Refresh { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        var uploader = YouTubeUploader.Get(UploaderId, new() {
            Refresh = Refresh
        });
        var originalUploader = uploader?.Clone();

        var rawItems = Names.ToList();
        var namesToAdd = new List<string>();
        var linksToAdd = new List<string>();

        foreach (var item in rawItems) {
            if (IsChannelIdentifier(item)) {
                linksToAdd.Add(NormalizeId(item));
            } else {
                namesToAdd.Add(item);
            }
        }

        if (uploader == null) {
            string? actualId;
            string? inferredName = null;

            if (IsChannelIdentifier(UploaderId)) {
                actualId = NormalizeId(UploaderId);
            } else {
                inferredName = UploaderId;
                var foundId = FindActualIdFromSearch(UploaderId);
                var suggestedId = foundId ?? ("@" + UploaderId.Replace(" ", "").ToLowerInvariant());

                actualId = AutoConfirmDefault
                    ? suggestedId
                    : Confirm(
                        $"Cannot find uploader for '{UploaderId}'. Please confirm or enter the uploader ID (handle or channel ID):",
                        suggestedId);
            }

            if (string.IsNullOrWhiteSpace(actualId)) {
                Logger.Fatal("No uploader ID provided.");
                return 1;
            }

            actualId = NormalizeId(actualId);

            uploader = YouTubeUploader.Get(actualId, new() {
                Refresh = Refresh
            });
            if (uploader == null) {
                uploader = new YouTubeUploader {
                    Id = actualId,
                    Name = inferredName
                };
            } else if (inferredName != null) {
                namesToAdd.Insert(0, inferredName);
            }
        }

        if (AsCanonical && namesToAdd.Count > 0) {
            var primary = namesToAdd[0];
            if (uploader.Name != null && uploader.Name != primary) {
                uploader.NameAliases.Add(uploader.Name);
            }

            uploader.Name = primary;
            uploader.NameAliases.Remove(primary);
            namesToAdd = namesToAdd.Skip(1).ToList();
        }

        foreach (var name in namesToAdd) {
            if (uploader.Name == null) {
                uploader.Name = name;
            } else if (uploader.Name != name) {
                uploader.NameAliases.Add(name);
            }
        }

        var linksToCreate = new List<string>();
        var standaloneToMerge = new List<(string linkId, YouTubeUploader existing)>();

        foreach (var linkId in linksToAdd.Distinct()) {
            if (linkId == uploader.Id) {
                Logger.Warn($"Skipped {linkId} as it is the target itself.");
                continue;
            }

            var existingLink = YouTubeUploader.Client.Get(linkId);
            if (existingLink?.Metadata?.Linking?.Target == uploader.Id) {
                Logger.Info($"{linkId} is already linked to {uploader.Id}.");
                continue;
            }

            if (existingLink != null && existingLink.RealId == linkId) {
                if (!string.IsNullOrEmpty(existingLink.Name) && existingLink.Name != uploader.Name) {
                    uploader.NameAliases.Add(existingLink.Name);
                }

                foreach (var alias in existingLink.NameAliases) {
                    if (alias != uploader.Name) {
                        uploader.NameAliases.Add(alias);
                    }
                }

                standaloneToMerge.Add((linkId, existingLink));
            }

            linksToCreate.Add(linkId);
        }

        var isNew = originalUploader == null;
        var summaryParts = new List<string>();
        if (uploader.Name != null) {
            summaryParts.Add($"Name: '{uploader.Name}'");
        }

        if (uploader.ChannelId != null) {
            summaryParts.Add($"ChannelId: '{uploader.ChannelId}'");
        }

        if (uploader.NameAliases.Count > 0) {
            summaryParts.Add($"Aliases: [{string.Join(", ", uploader.NameAliases)}]");
        }

        if (linksToCreate.Count > 0) {
            summaryParts.Add($"Links: [{string.Join(", ", linksToCreate)}]");
        }

        var summaryStr = summaryParts.Count > 0 ? string.Join(", ", summaryParts) : "no attributes";
        var confirmPrompt = isNew
            ? $"Confirm adding new uploader '{uploader.Id}' with {summaryStr}?"
            : $"Confirm updating uploader '{uploader.Id}' with {summaryStr}?";

        if (!AutoConfirmDefault && !Confirm(confirmPrompt, true)) {
            Logger.Info("Action cancelled by user.");
            return 0;
        }

        YouTubeUploader.Client.Set(uploader);

        foreach (var (linkId, _) in standaloneToMerge) {
            YouTubeUploader.Client.Delete(linkId);
        }

        foreach (var linkId in linksToCreate) {
            var result = YouTubeUploader.Client.Link(uploader.Id.Checked(), linkId);
            if (result.Status == KifaActionStatus.OK) {
                Logger.Info($"Successfully linked {linkId} -> {uploader.Id}.");
            } else {
                Logger.Error($"Failed to link {linkId} -> {uploader.Id}: {result.Message}");
            }
        }

        Logger.Info(
            $"Successfully {(isNew ? "added" : "updated")} uploader {uploader.Id} ({summaryStr}).");
        return 0;
    }

    static bool IsChannelIdentifier(string input) {
        if (string.IsNullOrWhiteSpace(input)) {
            return false;
        }

        var trimmed = input.Trim();
        return trimmed.StartsWith("@") ||
               trimmed.StartsWith("UC", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    static string NormalizeId(string id) {
        id = id.Trim();
        if (id.StartsWith("UC", StringComparison.OrdinalIgnoreCase) ||
            id.StartsWith("http", StringComparison.OrdinalIgnoreCase)) {
            return id;
        }

        return id.StartsWith("@") ? id : $"@{id}";
    }

    static string? FindActualIdFromSearch(string query) {
        try {
            var options = YouTubeVideo.GetOptionSet(flatPlaylist: true);
            var result = YouTubeVideo.YoutubeDL.RunVideoDataFetch($"ytsearch1:{query}", overrideOptions: options)
                .GetAwaiter().GetResult();
            if (result.Success && result.Data != null) {
                var entry = result.Data.Entries?.FirstOrDefault() ?? result.Data;
                var uploaderId = entry.UploaderID;
                if (!string.IsNullOrEmpty(uploaderId)) {
                    return uploaderId.StartsWith("@") ? uploaderId : "@" + uploaderId;
                }

                if (!string.IsNullOrEmpty(entry.ChannelID)) {
                    return entry.ChannelID;
                }
            }
        } catch (Exception ex) {
            Logger.Debug(ex, $"Failed to search YouTube for uploader ID of '{query}'.");
        }

        return null;
    }
}
