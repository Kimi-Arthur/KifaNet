using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Renci.SshNet;

namespace Pimix.IO {
    public class ServerConfig {
        public bool Removed { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public string RemotePrefix { get; set; }
        public string Prefix { get; set; }
    }

    public class FileStorageClient : StorageClient {
        const int DefaultBlockSize = 32 << 20;

        public static bool NeverLink { get; set; } = false;

        public static Dictionary<string, ServerConfig> ServerConfigs { get; set; } =
            new Dictionary<string, ServerConfig>();

        public override string ToString() => $"local:{ServerId}";

        string serverId;

        public string ServerId {
            get => serverId;
            set {
                serverId = value;
                Server = ServerConfigs.GetValueOrDefault(serverId, null);
            }
        }

        public ServerConfig Server { get; set; }

        static bool IsUnixLike
            => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
               RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public override void Copy(string sourcePath, string destinationPath) {
            Directory.GetParent(GetPath(destinationPath)).Create();

            if (NeverLink) {
                File.Copy(GetPath(sourcePath), GetPath(destinationPath));
            } else if (Server.RemotePrefix != null) {
                RemoteLink(GetRemotePath(sourcePath), GetRemotePath(destinationPath));
            } else {
                Link(GetPath(sourcePath), GetPath(destinationPath));
            }
        }

        void RemoteLink(string sourcePath, string destinationPath) {
            var connectionInfo = new ConnectionInfo(Server.Host,
                Server.Username,
                new PasswordAuthenticationMethod(Server.Username, Server.Password));

            using (var client = new SshClient(connectionInfo)) {
                client.Connect();
                var result = client.RunCommand($"ln \"{sourcePath}\" \"{destinationPath}\"");
                if (result.ExitStatus != 0) {
                    throw new Exception("Remote link command failed");
                }
            }
        }

        void Link(string sourcePath, string destinationPath) {
            using (var proc = new Process()) {
                proc.StartInfo.FileName = IsUnixLike ? "ln" : "cmd.exe";
                proc.StartInfo.Arguments = IsUnixLike
                    ? $"\"{sourcePath}\" \"{destinationPath}\""
                    : $"/c mklink /h \"{destinationPath}\" \"{sourcePath}\"";
                proc.StartInfo.UseShellExecute = false;
                proc.Start();
                proc.WaitForExit();
                if (proc.ExitCode != 0) {
                    throw new Exception("Local link command failed");
                }
            }
        }

        public override void Delete(string path) => File.Delete(GetPath(path));

        public override void Move(string sourcePath, string destinationPath)
            => File.Move(GetPath(sourcePath), GetPath(destinationPath));

        public override bool Exists(string path) => !Server.Removed && File.Exists(GetPath(path));

        public override IEnumerable<FileInformation> List(string path, bool recursive = false,
            string pattern = "*") {
            var normalizedPath = GetPath(path);
            if (!Directory.Exists(normalizedPath)) {
                return Enumerable.Empty<FileInformation>();
            }

            var directory = new DirectoryInfo(normalizedPath);
            var items = directory.GetFiles(pattern,
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            return items.OrderBy(i => i.Name).Select(i => new FileInformation {
                Id = GetId(i.FullName),
                Size = i.Length
            });
        }

        public override Stream OpenRead(string path) {
            var localPath = GetPath(path);
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

            using (var st = File.OpenRead(localPath)) {
                st.Seek(offset, SeekOrigin.Begin);
                return st.Read(buffer, 0, count);
            }
        }

        public override void Write(string path, Stream stream) {
            var blockSize = DefaultBlockSize;
            path = GetPath(path);
            Directory.GetParent(path).Create();
            using (var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
                fs.Seek(fs.Length.RoundDown(blockSize), SeekOrigin.Begin);
                stream.Seek(fs.Position, SeekOrigin.Begin);
                stream.CopyTo(fs, blockSize);
            }
        }

        string GetId(string path) => path.Substring(Server.Prefix.Length).Replace("\\", "/");

        string GetPath(string path) => $"{Server.Prefix}{path}";

        string GetRemotePath(string path) => $"{Server.RemotePrefix}{path}";
    }
}
