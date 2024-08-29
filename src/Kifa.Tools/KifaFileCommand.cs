using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using NLog;
using Kifa.Api.Files;
using Kifa.IO;
using Kifa.Jobs;

namespace Kifa.Tools;

public abstract partial class KifaFileCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target file(s) to take action on.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('i', "id", HelpText = "Treat input files as logical ids.")]
    public virtual bool ById { get; set; } = false;

    [Option('r', "recursive", HelpText = "Take action on files in recursive folders.")]
    public virtual bool Recursive { get; set; } = false;

    /// <summary>
    ///     Iterate over files with this prefix. If it's not, prefix the path with this member.
    /// </summary>
    protected virtual string Prefix => null;

    /// <summary>
    ///     By default, it will only iterate over existing files. When it's set to true, it will iterate over
    ///     logical ones and produce ExecuteOneInstance calls with the two combined.
    /// </summary>
    protected virtual bool IterateOverLogicalFiles => false;

    protected virtual bool NaturalSorting => false;

    public override int Execute(KifaTask? task = null) {
        var multi = FileNames.Count() > 1;
        if (ById || IterateOverLogicalFiles) {
            var fileInfos = new List<(string sortKey, string value)>();
            foreach (var fileName in FileNames) {
                var host = "";
                var path = fileName;
                if (IterateOverLogicalFiles) {
                    var f = new KifaFile(path);
                    if (!ById) {
                        host = f.Host;
                    }

                    path = f.Id;
                }

                path = Prefix == null ? path : $"{Prefix}{path}";
                var thisFolder = FileInformation.Client.ListFolder(path, Recursive);
                if (thisFolder.Count > 0) {
                    multi = true;
                    fileInfos.AddRange(thisFolder.Select(f => (f.GetNaturalSortKey(), host + f)));
                } else {
                    fileInfos.Add((path.GetNaturalSortKey(), host + path));
                }
            }

            fileInfos.Sort();

            var files = fileInfos.Select(f => f.value).ToList();
            return ById
                ? ExecuteAllFileInformation(files, multi)
                : ExecuteAllKifaFiles(files.Select(f => new KifaFile(f)).ToList(), multi);
        } else {
            var files = new List<(string sortKey, KifaFile value)>();
            foreach (var fileName in FileNames) {
                var fileInfo = new KifaFile(fileName);
                if (Prefix != null && !fileInfo.Path.StartsWith(Prefix)) {
                    fileInfo = fileInfo.GetFilePrefixed(Prefix);
                }

                var thisFolder = fileInfo.List(Recursive).ToList();
                if (thisFolder.Count > 0) {
                    multi = true;
                    files.AddRange(thisFolder.Select(f => (f.ToString().GetNaturalSortKey(), f)));
                } else {
                    files.Add((fileInfo.ToString().GetNaturalSortKey(), fileInfo));
                }
            }

            files.Sort();

            return ExecuteAllKifaFiles(files.Select(f => f.value).ToList(), multi);
        }
    }
}

public abstract partial class KifaFileCommand {
    protected virtual Func<List<string>, string> FileInformationConfirmText => null;
    protected virtual int ExecuteOneFileInformation(string file) => -1;

    int ExecuteAllFileInformation(List<string> files, bool multi) {
        if (multi && FileInformationConfirmText != null) {
            files.ForEach(Console.WriteLine);
            Console.Write(FileInformationConfirmText(files));
            Console.ReadLine();
        }

        var errors = new Dictionary<string, Exception>();
        var result = files.Select(s => {
            try {
                return ExecuteOneFileInformation(s);
            } catch (Exception ex) {
                Logger.Error($"{s}: {ex}");
                errors[s] = ex;
                return 255;
            }
        }).Max();

        foreach (var (key, value) in errors) {
            Logger.Error($"{key}: {value}");
        }

        if (errors.Count > 0) {
            Logger.Error($"{errors.Count} files failed to be taken action on.");
        }

        return result;
    }
}

public abstract partial class KifaFileCommand {
    protected virtual Func<List<KifaFile>, string> KifaFileConfirmText => null;
    protected virtual int ExecuteOneKifaFile(KifaFile file) => -1;

    int ExecuteAllKifaFiles(List<KifaFile> files, bool multi) {
        if (multi && KifaFileConfirmText != null) {
            files.ForEach(Console.WriteLine);
            Console.Write(KifaFileConfirmText(files));
            Console.ReadLine();
        }

        var errors = new Dictionary<string, Exception>();
        var result = files.Select(s => {
            try {
                return ExecuteOneKifaFile(s);
            } catch (Exception ex) {
                Logger.Error($"{s}: {ex}");
                errors[s.ToString()] = ex;
                return 255;
            }
        }).Max();

        foreach (var (key, value) in errors) {
            Logger.Error($"{key}: {value}");
        }

        if (errors.Count > 0) {
            Logger.Error($"{errors.Count} files failed to be taken action on.");
        }

        return result;
    }
}
