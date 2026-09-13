using CommandLine;
using Kifa.Jobs;
using Kifa.YouTube;
using NLog;

namespace Kifa.Tools.YoutubeUtil.Commands;

[Verb("name", HelpText = "Add a name or alias for a YouTube uploader.")]
public class AddUploaderNameCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Uploader ID, handle, or name.")]
    public string UploaderId { get; set; } = "";

    [Value(1, Required = false, HelpText = "Name(s) to add for the uploader.")]
    public IEnumerable<string> Names { get; set; } = [];

    [Option('c', "canonical", HelpText = "Set the (first) name as the canonical Name instead of an alias.")]
    public bool AsCanonical { get; set; } = false;

    [Option('r', "refresh", HelpText = "Force refresh server data before adding the name.")]
    public bool Refresh { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        var uploader = YouTubeUploader.Get(UploaderId, refresh: Refresh);
        var originalUploader = uploader?.Clone();

        var namesToAdd = Names.ToList();

        if (uploader == null) {
            string? actualId;
            string? inferredName = null;

            if (UploaderId.StartsWith("@") || UploaderId.StartsWith("UC", StringComparison.OrdinalIgnoreCase) ||
                UploaderId.StartsWith("http", StringComparison.OrdinalIgnoreCase)) {
                actualId = UploaderId;
            } else {
                inferredName = UploaderId;
                var foundId = FindActualIdFromSearch(UploaderId);
                var suggestedId = foundId ?? ("@" + UploaderId.Replace(" ", "").ToLowerInvariant());

                actualId = Confirm(
                    $"Cannot find uploader for '{UploaderId}'. Please confirm or enter the uploader ID (handle or channel ID):",
                    suggestedId);
            }

            if (string.IsNullOrWhiteSpace(actualId)) {
                Logger.Fatal("No uploader ID provided.");
                return 1;
            }

            actualId = actualId.Trim();
            if (!actualId.StartsWith("@") && !actualId.StartsWith("UC", StringComparison.OrdinalIgnoreCase)) {
                actualId = "@" + actualId;
            }

            uploader = YouTubeUploader.Get(actualId, refresh: Refresh);
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

        var isNew = originalUploader == null;
        var confirmPrompt = isNew
            ? $"Confirm adding new uploader '{uploader.Id}' with Name: '{uploader.Name}', Aliases: [{string.Join(", ", uploader.NameAliases)}]?"
            : $"Confirm updating uploader '{uploader.Id}' with Name: '{uploader.Name}', Aliases: [{string.Join(", ", uploader.NameAliases)}]?";

        if (!Confirm(confirmPrompt, true)) {
            Logger.Info("Action cancelled by user.");
            return 0;
        }

        YouTubeUploader.Client.Set(uploader);
        Logger.Info(
            $"Successfully {(isNew ? "added" : "updated")} uploader {uploader.Id} with Name: '{uploader.Name}', NameAliases: [{string.Join(", ", uploader.NameAliases)}].");
        return 0;
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
