using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Api.Files;
using Kifa.IO;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.FileUtil;

public abstract class KifaFileCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static List<FileInformation> FindFileInfosByIds(IEnumerable<string> sources,
        bool recursive = true) {
        var fileIds = sources.SelectMany(f => FileInformation.Client.ListFolder(f, recursive))
            .Distinct().OrderBy(f => f.GetNaturalSortKey()).ToList();
        var infos = FileInformation.Client.Get(fileIds);
        return fileIds.Zip(infos).Select(item => item.Second ?? new FileInformation {
            Id = item.First
        }).ToList();
    }

    public static List<FileInformation> FindFileInfos(IEnumerable<string> sources,
        bool byId = false, bool recursive = true, string pattern = "*", bool ignoreFiles = true)
        => byId
            ? FindFileInfosByIds(sources, recursive)
            : KifaFile.FindPotentialFiles(sources, recursive, pattern, ignoreFiles).Select(f
                => f.FileInfo ?? new FileInformation {
                    Id = f.Id
                }).DistinctBy(f => f.Id).OrderBy(f => f.Id.Checked().GetNaturalSortKey()).ToList();

    public List<KifaFile> RegisterUnregisteredFiles(List<KifaFile> files, bool showSize = false,
        string actionVerb = "processing") {
        var notRegisteredFiles = files.Where(f => !f.Registered).ToList();
        if (notRegisteredFiles.Count == 0) {
            return files;
        }

        var toRegister = SelectMany(notRegisteredFiles,
            file => showSize && file.FileInfo?.Size != null
                ? $"{file} ({file.FileInfo.Size.ToSizeString()})"
                : file.ToString(),
            new Func<List<KifaFile>, string>(choices
                => $"files{(showSize ? $" ({choices.Sum(c => c.FileInfo?.Size ?? 0).ToSizeString()})" : "")} to register before {actionVerb}"));

        if (toRegister.Status == KifaActionStatus.OK) {
            foreach (var f in toRegister.Value) {
                ExecuteItem($"register {f}", () => f.Add());
            }
        } else {
            ExecuteItem("files to register", () => toRegister);
        }

        return files.Where(f => f.Registered).ToList();
    }
}
