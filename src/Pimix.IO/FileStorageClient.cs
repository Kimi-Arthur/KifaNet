using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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

        string serverId;

        public static Dictionary<string, ServerConfig> ServerConfigs { get; set; } =
            new Dictionary<string, ServerConfig>();

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

        public override string ToString() => $"local:{ServerId}";

        public override void Copy(string sourcePath, string destinationPath,
            bool neverLink = false) {
            Directory.GetParent(GetPath(destinationPath)).Create();

            if (neverLink) {
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
                    throw new Exception("Remote link command failed: " + result.Result);
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

        public override void Delete(string path) {
            path = GetPath(path);
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }

        public override void Touch(string path) {
            path = GetPath(path);
            EnsureParent(path);

            File.Create(path).Close();
        }

        public override void Move(string sourcePath, string destinationPath) {
            destinationPath = GetPath(destinationPath);
            EnsureParent(destinationPath);

            File.Move(GetPath(sourcePath), destinationPath);
        }

        public override long Length(string path) {
            if (Server.Removed) {
                return -1;
            }

            var info = new FileInfo(GetPath(path));
            return info.Exists ? info.Length : -1;
        }

        public override IEnumerable<FileInformation> List(string path, bool recursive = false) {
            var normalizedPath = GetPath(path);
            if (!Directory.Exists(normalizedPath)) {
                return Enumerable.Empty<FileInformation>();
            }

            var directory = new DirectoryInfo(normalizedPath);
            var items = directory.GetFiles("*",
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            return items.OrderBy(i => i.Name.Normalize(NormalizationForm.FormC)).Select(i => {
                try {
                    return new FileInformation {
                        Id = GetId(i.FullName.Normalize(NormalizationForm.FormC)),
                        Size = i.Length
                    };
                } catch (Exception) {
                    return new FileInformation {
                        Id = GetId(i.FullName.Normalize(NormalizationForm.FormC)),
                        Size = 0
                    };
                }
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
            EnsureParent(path);
            using (var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
                fs.Seek(fs.Length.RoundDown(blockSize), SeekOrigin.Begin);
                if (fs.Position != 0) {
                    stream.Seek(fs.Position, SeekOrigin.Begin);
                }

                stream.CopyTo(fs, blockSize);
            }
        }

        string GetId(string path) => path.Substring(Server.Prefix.Length).Replace("\\", "/");

        string GetPath(string path) => $"{Server.Prefix}{path}";

        void EnsureParent(string path) {
            Directory.GetParent(path).Create();
        }

        string GetRemotePath(string path) => $"{Server.RemotePrefix}{path}";
    }
}
