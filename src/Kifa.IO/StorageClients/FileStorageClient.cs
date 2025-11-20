using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using NLog;
using Renci.SshNet;

namespace Kifa.IO.StorageClients;

public class ServerConfig {
    #region public late string Prefix { get; set; }

    string? prefix;

    public string Prefix {
        get => Late.Get(prefix);
        set => Late.Set(ref prefix, value);
    }

    #endregion

    public RemoteServerConfig? RemoteServer { get; set; }

    public string GetId(string actualPath) => actualPath[Prefix.Length..].Replace('\\', '/');

    public string GetPath(string path) => $"{Prefix}{path}";
}

public class RemoteServerConfig {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #region public late string Username { get; set; }

    string? username;

    public string Username {
        get => Late.Get(username);
        set => Late.Set(ref username, value);
    }

    #endregion

    #region public late string Password { get; set; }

    string? password;

    public string Password {
        get => Late.Get(password);
        set => Late.Set(ref password, value);
    }

    #endregion

    #region public late string Host { get; set; }

    string? host;

    public string Host {
        get => Late.Get(host);
        set => Late.Set(ref host, value);
    }

    #endregion

    #region public late string RemotePrefix { get; set; }

    string? remotePrefix;

    public string RemotePrefix {
        get => Late.Get(remotePrefix);
        set => Late.Set(ref remotePrefix, value);
    }

    #endregion

    public string GetRemotePath(string path) => $"{RemotePrefix}{path}";

    public void RemoteLink(string sourcePath, string destinationPath) {
        var connectionInfo = new ConnectionInfo(Host, Username,
            new PasswordAuthenticationMethod(Username, Password));

        using var client = new SshClient(connectionInfo);
        client.Connect();
        try {
            var result =
                client.RunCommand(
                    $"ln \"{GetRemotePath(sourcePath)}\" \"{GetRemotePath(destinationPath)}\"");
            Logger.Trace($"stdout: {new StreamReader(result.OutputStream).ReadToEnd()}");

            if (result.ExitStatus != 0) {
                Logger.Warn($"Failed to remote link: {result.Result}");
                throw new Exception("Remote link command failed: " + result.Result);
            }
        } catch (Exception ex) {
            Logger.Warn(ex, "Failed to remote link");
            throw;
        }
    }
}

public class FileStorageClient(string serverId) : StorageClient {
    const int DefaultBlockSize = 32 << 20;

    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static Dictionary<string, ServerConfig> ServerConfigs { get; set; } = new();

    string ServerId { get; } = serverId;

    ServerConfig? Server { get; } = ServerConfigs.GetValueOrDefault(serverId);

    static bool IsUnixLike
        => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
           RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    public override void Copy(string sourcePath, string destinationPath, bool neverLink = false) {
        if (Server == null) {
            throw new FileNotFoundException($"Server {ServerId} is not found in config.");
        }

        var actualSourcePath = Server.GetPath(sourcePath);
        var actualDestinationPath = Server.GetPath(destinationPath);

        Logger.Trace($"Copy {actualSourcePath} to {actualDestinationPath}");

        // Parent is created even for the remote case as we don't use remote to create parent.
        EnsureParent(actualDestinationPath);

        if (neverLink) {
            Logger.Trace($"Use raw file copying.");
            File.Copy(actualSourcePath, actualDestinationPath);
        } else if (Server.RemoteServer != null) {
            Logger.Trace($"Use remote linking.");
            Server.RemoteServer.RemoteLink(sourcePath, destinationPath);
        } else {
            Logger.Trace($"Use local linking.");
            Link(actualSourcePath, actualDestinationPath);
        }

        Logger.Trace($"Copying succeeded.");
    }

    static void Link(string actualSourcePath, string actualDestinationPath) {
        using var proc = new Process {
            StartInfo = {
                FileName = IsUnixLike ? "ln" : "cmd.exe",
                Arguments = IsUnixLike
                    ? $"\"{EscapeQuote(actualSourcePath)}\" \"{EscapeQuote(actualDestinationPath)}\""
                    : $"/c mklink /h \"{EscapeQuote(actualDestinationPath)}\" \"{EscapeQuote(actualSourcePath)}\"",
                UseShellExecute = false
            }
        };
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.RedirectStandardError = true;
        proc.Start();
        proc.WaitForExit();
        Logger.Trace($"stdout: {proc.StandardOutput.ReadToEnd()}");
        Logger.Trace($"stderr: {proc.StandardError.ReadToEnd()}");
        if (proc.ExitCode != 0) {
            Logger.Warn($"Failed to local link.");
            throw new Exception("Local link command failed");
        }
    }

    static string EscapeQuote(string s) => s.Replace("\"", "\\\"");

    public override void Delete(string path) {
        if (Server == null) {
            Logger.Warn($"Server {ServerId} is not found in config.");
            return;
        }

        path = Server.GetPath(path);
        if (File.Exists(path)) {
            File.Delete(path);
        }
    }

    public override void Touch(string path) {
        if (Server == null) {
            throw new FileNotFoundException($"Server {ServerId} is not found in config.");
        }

        path = Server.GetPath(path);
        EnsureParent(path);

        File.Create(path).Close();
    }

    public override void Move(string sourcePath, string destinationPath) {
        if (Server == null) {
            throw new FileNotFoundException($"Server {ServerId} is not found in config.");
        }

        destinationPath = Server.GetPath(destinationPath);
        EnsureParent(destinationPath);

        File.Move(Server.GetPath(sourcePath), destinationPath);
    }

    public override long Length(string path) {
        if (Server == null) {
            throw new FileNotFoundException();
        }

        var info = new FileInfo(Server.GetPath(path));
        return info.Exists ? info.Length : throw new FileNotFoundException();
    }

    public override IEnumerable<FileInformation> List(string path, bool recursive = false) {
        if (Server == null) {
            throw new FileNotFoundException($"Server {ServerId} is not found in config.");
        }

        var normalizedPath = Server.GetPath(path);
        if (!Directory.Exists(normalizedPath)) {
            return Enumerable.Empty<FileInformation>();
        }

        var directory = new DirectoryInfo(normalizedPath);
        var items = directory.GetFiles("*",
            recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        return items.OrderBy(i => i.Name.Normalize(NormalizationForm.FormC)).Select(i => {
            try {
                return new FileInformation {
                    Id = Server.GetId(i.FullName.Normalize(NormalizationForm.FormC)),
                    Size = i.Length
                };
            } catch (Exception) {
                return new FileInformation {
                    Id = Server.GetId(i.FullName.Normalize(NormalizationForm.FormC)),
                    Size = 0
                };
            }
        });
    }

    public override Stream OpenRead(string path) {
        if (Server == null) {
            throw new FileNotFoundException($"Server {ServerId} is not found in config.");
        }

        var localPath = Server.GetPath(path);
        var fileSize = new FileInfo(localPath).Length;
        return new SeekableReadStream(fileSize,
            (buffer, bufferOffset, offset, count)
                => Read(buffer, localPath, bufferOffset, offset, count));
    }

    int Read(byte[] buffer, string localPath, int bufferOffset = 0, long offset = 0,
        int count = -1) {
        if (count < 0) {
            count = buffer.Length - bufferOffset;
        }

        using var st = File.OpenRead(localPath);
        st.Seek(offset, SeekOrigin.Begin);
        return st.Read(buffer, 0, count);
    }

    public override void Write(string path, Stream stream) {
        if (Server == null) {
            throw new FileNotFoundException($"Server {ServerId} is not found in config.");
        }

        if (Exists(path)) {
            Logger.Debug($"Target file {path} already exists. Skipped.");
            return;
        }

        var localPath = Server.GetPath(path);

        var tempPath = $"{localPath}.tmp";

        Logger.Trace($"Copying to {tempPath}...");

        var blockSize = DefaultBlockSize;
        EnsureParent(tempPath);

        // FileShare.None is used so that file is locked when writing.
        var options = !File.Exists(tempPath)
            ? new FileStreamOptions {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                PreallocationSize = stream.Length,
                Share = FileShare.None
            }
            : new FileStreamOptions {
                Mode = FileMode.Open,
                Access = FileAccess.Write,
                Share = FileShare.None
            };

        using (var fs = new FileStream(tempPath, options)) {
            fs.Seek(fs.Length.RoundDown(blockSize), SeekOrigin.Begin);
            if (fs.Position != 0) {
                stream.Seek(fs.Position, SeekOrigin.Begin);
            }

            stream.CopyTo(fs, blockSize);

            Logger.Trace($"Finished copying to {tempPath}.");
        }

        File.Move(tempPath, localPath);
        Logger.Trace($"Moved to final destination {path}.");
    }

    public override string Type => "local";

    public override string Id => ServerId;

    public static void EnsureParent(string actualPath) {
        var parent = Directory.GetParent(actualPath);
        if (parent == null) {
            throw new FileNotFoundException($"Cannot find parent folder for {actualPath}");
        }

        parent.Create();
    }

    public string GetLocalPath(string path) {
        return Server?.GetPath(path) ??
               throw new FileNotFoundException($"Server {ServerId} is not found in config.");
    }

    public override FileIdInfo? GetFileIdInfo(string path) {
        if (!IsUnixLike) {
            return null;
        }

        var localPath = GetLocalPath(path);
        if (!File.Exists(localPath)) {
            return null;
        }

        var id = UnixFileInfo.GetInode(localPath);
        if (id == null) {
            return null;
        }

        var info = new FileInfo(localPath);
        var lastModified = info.LastWriteTimeUtc;

        // Remove unencodable datetime part like sub-microsecond component.
        lastModified = lastModified.Clone();

        Logger.Trace($"{path} has inode of {id}");
        return new() {
            InternalFildId = id.ToString(),
            Size = info.Length,
            LastModified = lastModified
        };
    }

    public override ulong? GetFileRefCount(string path) {
        if (!IsUnixLike) {
            return null;
        }

        var localPath = GetLocalPath(path);
        if (!File.Exists(localPath)) {
            return null;
        }

        return UnixFileInfo.GetRefCount(localPath);
    }
}
