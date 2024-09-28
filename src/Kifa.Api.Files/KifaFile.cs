using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Kifa.Cloud.BaiduCloud;
using Kifa.Cloud.Google;
using Kifa.Cloud.MegaNz;
using Kifa.Cloud.Swisscom;
using Kifa.Cloud.Telegram;
using Kifa.IO;
using Kifa.IO.FileFormats;
using Kifa.IO.StorageClients;
using Kifa.Service;
using NLog;

namespace Kifa.Api.Files;

public partial class KifaFile : IComparable<KifaFile>, IEquatable<KifaFile>, IDisposable {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static FileInformationServiceClient? fileInfoClient;

    static FileInformationServiceClient FileInfoClient
        => (fileInfoClient ??= FileInformation.Client).Checked();

    #region Configs

    public static string LocalServer { get; set; }

    public static string DefaultIgnoredPrefix { get; set; } = "@";

    public static HashSet<string> IgnoredPrefixes { get; set; } = new() {
        DefaultIgnoredPrefix
    };

    public static HashSet<string> IgnoredExtensions { get; set; } = new();

    public static string IgnoredPattern { get; set; } = "$^";

    static Regex? ignoredFiles;

    static Regex IgnoredFiles => ignoredFiles ??= new Regex(IgnoredPattern, RegexOptions.Compiled);

    #endregion

    // Canonical `Id` that corresponds to a `FileInformation` object, without format etc.
    public string Id { get; set; }

    public FileInformation? FileInfo { get; set; }

    StorageClient Client { get; set; }

    public KifaFile Parent => new($"{Host}{ParentPath}");

    // Path = ParentPath/BaseName.Extension = ParentPath/Name

    // Ends with a slash.
    public string ParentPath { get; }
    public string BaseName { get; set; }
    public string? Extension { get; set; }
    public string Name => string.IsNullOrEmpty(Extension) ? BaseName : $"{BaseName}.{Extension}";
    public string Path => $"{ParentPath}{Name}";

    public string Host => Client.ToString();

    KifaFileFormat FileFormat { get; }

    public bool IsCloud
        => Client is BaiduCloudStorageClient or GoogleDriveStorageClient or MegaNzStorageClient &&
           FileFormat is KifaFileV1Format or KifaFileV2Format;

    public bool IsLocal => Client is FileStorageClient;

    public long Length {
        get {
            using var s = OpenRead();
            return s.Length;
        }
    }

    public bool Registered => FileInfo?.Locations.GetValueOrDefault(ToString(), null) != null;

    public bool Allocated => FileInfo?.Locations.ContainsKey(ToString()) ?? false;

    public bool HasEntry => FileInfo?.Locations.ContainsKey(ToString()) == true;

    bool UseCache { get; set; }

    string LocalFilePath { get; }
    KifaFile LocalFile => new(LocalFilePath);

    FileIdInfo? idInfo;
    public FileIdInfo? IdInfo => idInfo ??= Client.GetFileIdInfo(Path)?.With(f => f.HostId = Host);

    public string? FileId => IdInfo?.Id;

    public KifaFile(string? uri = null, string? id = null, FileInformation? fileInfo = null,
        bool useCache = false, HashSet<string>? allowedClients = null) {
        uri ??= GetUri(id ?? fileInfo!.Id, allowedClients);
        if (uri == null) {
            throw new FileNotFoundException();
        }

        uri = NormalizeUri(uri);

        var segments = uri.Split('/');
        ParentPath = "/" + string.Join("/", segments[1..^1]);
        if (!ParentPath.EndsWith("/")) {
            ParentPath += "/";
        }

        var name = segments.Last();
        var lastDot = name.LastIndexOf('.');
        if (lastDot < 0) {
            BaseName = name;
            Extension = "";
        } else {
            BaseName = name.Substring(0, lastDot);
            Extension = name.Substring(lastDot + 1);
        }

        Id = id ?? fileInfo?.Id ?? FileInformation.GetId(uri)!;
        FileInfo = fileInfo ?? FileInfoClient.Get(Id) ?? new FileInformation {
            Id = Id
        };
        if (Id != FileInfo.Id) {
            throw new FileCorruptedException($"File's ID doesn't match: {Id} vs {FileInfo?.Id}");
        }

        // Always store with its id makes more sense to me.
        LocalFilePath = $"{LocalServer}{Id}";

        Client = GetClient(segments[0]);

        FileFormat = KifaFileV2Format.Get(uri) ?? KifaFileV1Format.Get(uri) ??
            KifaFileV0Format.Get(uri) ?? RawFileFormat.Instance;
        UseCache = useCache;
    }

    // Supported uri examples:
    //   - Canonical paths (conversion goal):
    //     - baidu:Pimix_1/a/b/c/d.txt.v1
    //     - mega:0z/a/b/c/d.txt
    //     - local:cubie/a/b/c/d.txt
    //   local:/a/b/c/d.txt
    //   - Local absolute path
    //     - /a/b/c/d.txt => local:some_cell_a/b/c/d.txt
    //     - C:/files/a.txt => local:some_win_cell/a.txt
    //     - ~/a.txt => local:some_home/a.txt
    //     - ../a.txt => local:some_cell/path/to/parent/a.txt
    static string NormalizeUri(string uri) {
        if (uri.StartsWith("http://") || uri.StartsWith("https://")) {
            return uri;
        }

        // Local path, convert to canonical one.
        var fullPath = System.IO.Path.GetFullPath(uri).Replace('\\', '/');
        foreach (var p in FileStorageClient.ServerConfigs) {
            if (fullPath.StartsWith(p.Value.Prefix)) {
                return $"local:{p.Key}{fullPath[p.Value.Prefix.Length..]}";
            }
        }

        throw new FileNotFoundException($"Path {uri} is not valid.");
    }

    static string? GetUri(string id, HashSet<string>? allowedClients) {
        string? candidate = null;
        var bestScore = 0L;
        var info = FileInfoClient.Get(id);
        if (info != null) {
            foreach (var (location, verifyTime) in info.Locations) {
                if (verifyTime != null) {
                    var file = new KifaFile(location, fileInfo: info);
                    if (file.Client is FileStorageClient && file.Exists()) {
                        return location;
                    }

                    // Ignore clients not allowed.
                    if (allowedClients != null && !allowedClients.Contains(file.Client.Type)) {
                        continue;
                    }

                    var score = file.Client switch {
                        GoogleDriveStorageClient => 32,
                        WebStorageClient => 24,
                        SwisscomStorageClient => 16,
                        BaiduCloudStorageClient => 8,
                        _ => 0
                    } + file.FileFormat switch {
                        KifaFileV2Format => 4,
                        KifaFileV1Format => 2,
                        KifaFileV0Format => 1,
                        _ => 0
                    };

                    if (score > bestScore) {
                        bestScore = score;
                        candidate = location;
                    }
                }
            }
        }

        if (bestScore > 0) {
            return candidate;
        }

        foreach (var p in FileStorageClient.ServerConfigs.Keys) {
            var location = $"local:{p}{id}";
            var score = new KifaFile(location).Length;
            if (score > bestScore) {
                bestScore = score;
                candidate = location;
            }
        }

        return candidate;
    }

    public KifaFile GetFile(string name) => new($"{Host}{Path}/{name}");

    public KifaFile GetFilePrefixed(string prefix) => new($"{Host}{prefix}{Path}");

    public KifaFile GetIgnoredFile(string type)
        => Parent.GetFile($"{DefaultIgnoredPrefix}{BaseName}.{type}");

    public override string ToString() => $"{Host}{Path}";

    public bool Exists()
        => FileInfo?.Size != null && FileFormat is RawFileFormat
            ? Client.Exists(Path, FileInfo.Size.Value)
            : Client.Exists(Path);

    public bool ExistsSomewhere() => FileInfo?.Locations.Values.Any(v => v != null) == true;

    public Stream OpenRead()
        => new VerifiableStream(
            FileFormat.GetDecodeStream(Client.OpenRead(Path), FileInfo?.EncryptionKey), FileInfo);

    public void Write(Stream stream)
        => Client.Write(Path, FileFormat.GetEncodeStream(stream, FileInfo));

    public void Write(Func<Stream> getStream) {
        if (Exists()) {
            Logger.Debug($"Target file {this} already exists. Skipped.");
            return;
        }

        Write(getStream());
    }

    public void Write(byte[] data) => Write(new MemoryStream(data));

    public void Write(string text) => Write(new UTF8Encoding(false).GetBytes(text));

    public void Delete() => Client.Delete(Path);

    public void Touch() => Client.Touch(Path);

    public IEnumerable<KifaFile> List(bool recursive = false, bool ignoreFiles = true,
        string pattern = "*") {
        if (Exists()) {
            return Enumerable.Repeat(this, 1);
        }

        var files = Client.List(Path, recursive).Where(f
            => IsMatch(f.Id, pattern) && (!ignoreFiles || !ShouldIgnore(f.Id, Path))).ToList();
        var fileInfos = FileInfoClient.Get(files.Select(f => f.Id).ToList());
        return files.Zip(fileInfos).Select(item => new KifaFile(Host + item.First.Id,
            fileInfo: item.Second ?? new FileInformation {
                Id = item.First.Id
            }));
    }

    static bool ShouldIgnore(string logicalPath, string pathPrefix)
        => IgnoredExtensions.Any(ext => logicalPath.EndsWith($".{ext}")) ||
           IgnoredPrefixes.Any(prefix
               => logicalPath[pathPrefix.Length..].Split("/")
                   .Any(segment => segment.StartsWith(prefix))) ||
           IgnoredFiles.IsMatch(logicalPath);

    // KifaFile.FileInfo is filled for items returned.
    public static List<KifaFile> FindExistingFiles(IEnumerable<string> sources,
        string? prefix = null, bool recursive = true, string pattern = "*",
        bool ignoreFiles = true) {
        var files = new List<(string SortKey, KifaFile File)>();
        foreach (var source in GetKifaFiles(sources)) {
            var file = source;
            if (prefix != null && !file.Path.StartsWith(prefix)) {
                file = file.GetFilePrefixed(prefix);
            }

            var fileInfos = file.List(recursive, pattern: pattern, ignoreFiles: ignoreFiles)
                .ToList();
            Logger.Trace($"Found {fileInfos.Count} existing files:");
            foreach (var f in fileInfos) {
                Logger.Trace($"\t{f}");
            }

            files.AddRange(fileInfos.Select(f => (f.ToString().GetNaturalSortKey(), f)));
        }

        files.Sort();

        return files.Select(f => f.File).ToList();
    }

    // KifaFile.FileInfo is filled for items returned.
    public static List<KifaFile> FindPhantomFiles(IEnumerable<string> sources,
        string? prefix = null, bool recursive = true, bool ignoreFiles = true) {
        var files = FindPotentialFiles(sources, prefix, recursive, ignoreFiles);
        return files.Where(f => f.Registered && !f.Exists()).ToList();
    }

    // KifaFile.FileInfo is filled for items returned.
    public static List<KifaFile> FindPotentialFiles(IEnumerable<string> sources,
        string? prefix = null, bool recursive = true, bool ignoreFiles = true) {
        var files = new List<(string sortKey, string value)>();
        foreach (var source in GetKifaFiles(sources)) {
            var file = source;
            if (prefix != null && !file.Path.StartsWith(prefix)) {
                file = file.GetFilePrefixed(prefix);
            }

            var path = file.Path;
            var host = file.Host;

            var thisFolder = FileInfoClient.ListFolder(path, recursive);
            files.AddRange(thisFolder.Where(f => !ignoreFiles || !ShouldIgnore(f, file.Id))
                .Select(f => (f.GetNaturalSortKey(), host + f)));
        }

        files.Sort();

        return GetKifaFiles(files.Select(f => f.value)).ToList();
    }

    static IEnumerable<KifaFile> GetKifaFiles(IEnumerable<string> fileNames) {
        var files = fileNames.Select(NormalizeUri).ToList();
        var infos =
            FileInfoClient.Get(files.Select(f => FileInformation.GetId(f).Checked()).ToList());
        return files.Zip(infos).Select(source => new KifaFile(source.First,
            fileInfo: source.Second ?? new FileInformation {
                Id = FileInformation.GetId(source.First).Checked()
            }));
    }

    // KifaFile.FileInfo is filled for items returned.
    public static List<KifaFile> FindAllFiles(IEnumerable<string> sources, string? prefix = null,
        bool recursive = true, string pattern = "*", bool ignoreFiles = true) {
        var sourceFiles = sources.ToList();
        var existingFiles = FindExistingFiles(sourceFiles, prefix, recursive, pattern: pattern,
            ignoreFiles: ignoreFiles);
        var potentialFiles = FindPotentialFiles(sourceFiles, prefix, recursive,
            ignoreFiles: ignoreFiles);
        var allFiles = new HashSet<KifaFile>();
        allFiles.UnionWith(existingFiles);
        allFiles.UnionWith(potentialFiles);
        return allFiles.OrderBy(f => f.ToString().GetNaturalSortKey()).ToList();
    }

    public static KifaFile? FindOne(List<KifaFile> files)
        => files.Find(file => file.Exists() || file.ExistsSomewhere());

    public static void LinkAll(KifaFile source, List<KifaFile> links) {
        if (source.ExistsSomewhere()) {
            Logger.Debug("Source is alreay in system. Will link files virtually.");
            foreach (var link in links) {
                if (link.Equals(source)) {
                    continue;
                }

                FileInfoClient.Link(source.Id, link.Id);
                Logger.Debug($"Linked {link.Id} => {source.Id}.");
            }

            return;
        }

        Logger.Debug("Source is not in system. Will link files locally.");
        foreach (var link in links) {
            if (link.Exists()) {
                continue;
            }

            source.Copy(link);
            Logger.Debug($"Linked {link} => {source}.");
        }
    }

    static bool IsMatch(string path, string pattern) {
        path = path.Substring(path.LastIndexOf("/", StringComparison.Ordinal) + 1);
        var segments = pattern.Split("*", StringSplitOptions.RemoveEmptyEntries);
        var lastIndex = 0;
        foreach (var segment in segments) {
            lastIndex = path.IndexOf(segment, lastIndex, StringComparison.Ordinal);
            if (lastIndex < 0) {
                return false;
            }

            lastIndex += segment.Length;
        }

        return true;
    }

    public void Copy(KifaFile destination, bool neverLink = false) {
        if (UseCache) {
            CacheFileToLocal();
            LocalFile.Copy(destination, neverLink);
            return;
        }

        if (IsCompatible(destination)) {
            Client.Copy(Path, destination.Path, neverLink);
        } else {
            using var stream = OpenRead();
            destination.Write(stream);
        }
    }

    public void Move(KifaFile destination) {
        if (destination.Exists()) {
            throw new ArgumentException($"Destination {destination} already exists.");
        }

        if (UseCache) {
            CacheFileToLocal();
            LocalFile.Move(destination);
            return;
        }

        if (IsCompatible(destination)) {
            Client.Move(Path, destination.Path);
        } else {
            Copy(destination);
            Delete();
        }
    }

    void CacheFileToLocal() {
        if (!LocalFile.Registered || !LocalFile.Exists()) {
            Logger.Debug($"Caching {this} to {LocalFile}...");
            using var stream = OpenRead();
            LocalFile.Write(stream);
            LocalFile.Add();
        }
    }

    void RemoveLocalCacheFile() {
        if (UseCache && LocalFile.Exists()) {
            LocalFile.Delete();
            LocalFile.Unregister();
            Logger.Debug($"Removed cache file {LocalFile}...");
        }
    }

    public FileInformation CalculateInfo(FileProperties properties) {
        var info = FileInfo?.Clone() ?? new FileInformation {
            Id = Id
        };

        info.RemoveProperties(
            (FileProperties.AllVerifiable & properties) | FileProperties.Locations);

        using var stream = OpenRead();
        info.AddProperties(stream, properties);

        return info;
    }

    /// <summary>
    /// Register the file with optional check.
    /// </summary>
    /// <param name="shouldCheckKnown">
    /// If it's true, it will do a full checkup no matter what.<br/>
    /// If it's false, it will never do a check if the file is known.<br/>
    /// If it's null (default case), it will do a quick check for known instance.</param>
    /// <exception cref="FileNotFoundException">The file doesn't exist.</exception>
    /// <exception cref="FileCorruptedException">File check failed for known file.</exception>
    public void Add(bool? shouldCheckKnown = null) {
        if (shouldCheckKnown != false && !Exists()) {
            throw new FileNotFoundException(ToString());
        }

        var file = this;
        Logger.Debug($"Checking file {file}...");

        var oldInfo = FileInfo;

        if (UseCache) {
            try {
                LocalFile.Add();
                file = LocalFile;
                Logger.Debug($"Local file {file} is found. Use local file instead of {this}.");
            } catch (FileNotFoundException) {
                // Expected to find no cached file.
            }
        }

        if (shouldCheckKnown != true &&
            (oldInfo?.GetProperties() & FileProperties.All) == FileProperties.All &&
            file.Registered) {
            oldInfo = oldInfo.Checked();
            if (shouldCheckKnown == false) {
                Logger.Debug($"Quick check skipped for {file}.");
                return;
            }

            var quickInfo = file.CalculateInfo(FileProperties.Size);
            var compareResults = quickInfo.CompareProperties(oldInfo, FileProperties.AllVerifiable);
            if (compareResults != FileProperties.None) {
                throw new FileCorruptedException(
                    $"Quick result differs from old result: {compareResults}\n" + "Expected:\n" +
                    oldInfo.RemoveProperties(FileProperties.All ^ compareResults) + "\nActual:\n" +
                    quickInfo.RemoveProperties(FileProperties.All ^ compareResults));
            }

            Logger.Debug($"Quick check passed for {file}.");
            return;
        }

        if (UseCache) {
            CacheFileToLocal();
            file = LocalFile;
            Logger.Debug($"Since file is cached, use local file {file} instead now.");
        }

        // If full check is explicitly requested, we should still check.
        if (shouldCheckKnown != true && file.CheckedByFileId()) {
            Logger.Debug($"Skipping check for {file} as it's already checked by file_id.");
            Register(true);
            return;
        }

        // A new encryption key will be added if not already there.
        var info = file.CalculateInfo(FileProperties.AllVerifiable | FileProperties.EncryptionKey);

        var compareResultWithOld = info.CompareProperties(oldInfo, FileProperties.AllVerifiable);
        if (compareResultWithOld != FileProperties.None) {
            throw new FileCorruptedException(
                $"New result differs from old result: {compareResultWithOld}\n" + "Expected:\n" +
                oldInfo.RemoveProperties(FileProperties.All ^ compareResultWithOld) +
                "\nActual:\n" + info.RemoveProperties(FileProperties.All ^ compareResultWithOld));
        }

        // info.Sha256 is for sure available now.
        var sha256Info = FileInfoClient.Get($"/$/{info.Sha256}");

        if (sha256Info != null) {
            if (sha256Info.Sha256 == info.Sha256) {
                if (sha256Info.RealId != info.RealId) {
                    Logger.LogResult(FileInfoClient.Link(sha256Info.Id, info.Id),
                        $"linking file from {sha256Info.Id} to {info.Id} due common SHA256 value",
                        defaultLevel: LogLevel.Debug, throwIfError: true);
                } else {
                    Logger.Trace($"File {info.Id} already linked to {sha256Info.Id}");
                }
            } else {
                throw new FileCorruptedException(
                    $"UNEXPECTED: The file found by SHA256 doesn't match the file SHA256: {sha256Info.Sha256} vs {info.Sha256}");
            }
        } else {
            Logger.Trace($"No file with SHA256 {info.Sha256} is found.");
        }

        var compareResult = info.CompareProperties(sha256Info, FileProperties.AllVerifiable);
        if (compareResult == FileProperties.None) {
            // Respects the original encryption key.
            info.EncryptionKey = sha256Info?.EncryptionKey ?? info.EncryptionKey;

            // Even though `Locaions` is removed, it's still OK as it's `Update()`.
            FileInfoClient.Update(info);
            Register(true);
            RegisterFileIdInfo();
        } else {
            throw new FileCorruptedException(
                $"New result differs from sha256 result: {compareResult}\n" + "Expected:\n" +
                sha256Info.RemoveProperties(FileProperties.All ^ compareResult) + "\nActual:\n" +
                info.RemoveProperties(FileProperties.All ^ compareResult));
        }
    }

    bool CheckedByFileId() {
        if (FileId == null || IdInfo == null) {
            return false;
        }

        var existingIdInfo = FileIdInfo.Client.Get(FileId);
        if (existingIdInfo?.Sha256 == null) {
            Logger.Debug($"File of file_id {FileId} is not found.");
            return false;
        }

        if (IdInfo.Size != existingIdInfo.Size ||
            IdInfo.LastModified != existingIdInfo.LastModified) {
            Logger.Warn($"File of file_id {FileId} is found, but some info differs: " +
                        $"{existingIdInfo.Size} vs {IdInfo.Size}; {existingIdInfo.LastModified.ToJson()} vs {IdInfo.LastModified.ToJson()}");
            return false;
        }

        if (FileInfo?.Sha256 == null) {
            var sha256Info = FileInfoClient.Get($"/$/{existingIdInfo.Sha256}");

            // FileInfo is unknown. Just link it.
            if (sha256Info != null) {
                Logger.LogResult(FileInfoClient.Link(sha256Info.Id, Id),
                    "linking file as checked by file_id", throwIfError: true,
                    defaultLevel: LogLevel.Debug);
                FileInfo = FileInfoClient.Get(Id);

                return true;
            }

            Logger.Warn($"File of SHA256 {existingIdInfo.Sha256} should exist.");
            return false;
        }

        // FileInfo.Sha256 should present as this should be a known file.
        if (existingIdInfo.Sha256 != FileInfo?.Sha256) {
            Logger.Warn(
                $"SHA256 found by file_id {FileId} differs: {existingIdInfo.Sha256} vs {FileInfo?.Sha256}");
            return false;
        }

        Logger.Debug(
            $"File of file_id {FileId} is already checked with SHA256 of {existingIdInfo.Sha256}.");
        return true;
    }

    void RegisterFileIdInfo() {
        if (IdInfo != null) {
            IdInfo.Sha256 = FileInfo.Checked().Sha256;
            if (IdInfo.Sha256 == null) {
                throw new FileCorruptedException(
                    "UNEXPECTED: SHA256 field was not found when registering file_id.");
            }

            Logger.LogResult(FileIdInfo.Client.Set(IdInfo),
                $"registering {IdInfo.Id} to {IdInfo.Sha256}", defaultLevel: LogLevel.Debug,
                throwIfError: true);
        }
    }

    public void Register(bool verified = false) {
        FileInfoClient.AddLocation(Id, ToString(), verified);
        FileInfo = FileInfoClient.Get(Id);
    }

    public void Unregister() {
        FileInfoClient.RemoveLocation(Id, ToString());
        FileInfo = FileInfoClient.Get(Id);
    }

    public bool IsCompatible(KifaFile other)
        => Host == other.Host && FileFormat == other.FileFormat;

    public override int GetHashCode() => ToString()?.GetHashCode() ?? 0;

    public bool Equals(KifaFile? other) {
        if (ReferenceEquals(null, other)) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return string.Equals(ToString(), other.ToString());
    }

    public int CompareTo(KifaFile other) {
        if (ReferenceEquals(this, other)) {
            return 0;
        }

        if (ReferenceEquals(null, other)) {
            return 1;
        }

        return string.Compare(ToString(), other.ToString(), StringComparison.Ordinal);
    }

    public override bool Equals(object obj) {
        if (ReferenceEquals(null, obj)) {
            return false;
        }

        if (ReferenceEquals(this, obj)) {
            return true;
        }

        return obj.GetType() == GetType() && Equals((KifaFile) obj);
    }

    public string ReadAsString() {
        using var streamReader = new StreamReader(OpenRead());
        return streamReader.ReadToEnd();
    }

    public byte[] ReadAsBytes() => OpenRead().ToByteArray();

    static StorageClient GetClient(string spec) {
        var specs = spec.Split(':');

        switch (specs[0]) {
            case "baidu":
                return new BaiduCloudStorageClient {
                    AccountId = specs[1]
                };
            case "google":
                return GoogleDriveStorageClient.Create(specs[1]);
            case "mega":
                return new MegaNzStorageClient {
                    AccountId = specs[1]
                };
            case "swiss":
                return SwisscomStorageClient.Create(specs[1]);
            case "tele":
                return TelegramStorageClient.Create(specs[1]);
            case "http":
            case "https":
                return new WebStorageClient {
                    Protocol = specs[0]
                };
            case "local":
                return new FileStorageClient(specs[1]);
        }

        throw new Exception($"Failed to create client for {spec}");
    }

    public string GetLocalPath()
        => Client is FileStorageClient fileClient
            ? fileClient.GetLocalPath(Path)
            : throw new FileNotFoundException(
                "Should not try to get a local path with a non FileStorageClient.");

    public void EnsureLocalParent() => FileStorageClient.EnsureParent(GetLocalPath());

    public static KifaFile GetLocal(string path) => new($"{LocalServer}{path}");

    public void Dispose() {
        Client.Dispose();
    }

    public bool IsSameLocalFile(KifaFile source) {
        return FileId != null && FileId == source.FileId;
    }
}
