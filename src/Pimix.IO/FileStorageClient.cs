using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pimix.IO {
    public class FileStorageClient : StorageClient {
        const int DefaultBlockSize = 32 << 20;

        public static StorageClient Get(string fileSpec) {
            var specs = fileSpec.Split(';');
            foreach (var spec in specs)
                if (spec.StartsWith("local:"))
                    return new FileStorageClient {BasePath = spec.Substring(6)};

            return null;
        }

        public string BasePath { get; set; }

        public override string ToString() => $"local:{BasePath}";

        public override void Copy(string sourcePath, string destinationPath)
            => File.Copy(GetPath(sourcePath), GetPath(destinationPath));

        public override void Delete(string path) => File.Delete(GetPath(path));

        public override void Move(string sourcePath, string destinationPath)
            => File.Move(GetPath(sourcePath), GetPath(destinationPath));

        public override bool Exists(string path) => File.Exists(GetPath(path));

        public override IEnumerable<FileInformation> List(string path, bool recursive = false) {
            var directory = new DirectoryInfo(GetPath(path));
            var items = directory.GetFiles("*",
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            return items.OrderBy(i => i.Name).Select(i => new FileInformation() {
                Id = GetId(i.FullName),
                Size = i.Length
            });
        }

        private string GetId(string path) {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
                return path.Substring($"/allfiles/{BasePath}".Length);
            return path.Substring($"\\\\{BasePath}/files".Length).Replace("\\", "/");
        }

        public override Stream OpenRead(string path) => File.OpenRead(GetPath(path));

        public override void Write(string path, Stream stream) {
            var blockSize = DefaultBlockSize;
            path = GetPath(path);
            Directory.GetParent(path).Create();
            using (var fs = new FileStream(path, FileMode.Create)) {
                stream.CopyTo(fs, blockSize);
            }
        }

        string GetPath(string path) {
            if (BasePath == null) return path;

            if (Environment.OSVersion.Platform == PlatformID.Unix)
                return $"/allfiles/{BasePath}{path}";
            return $"\\\\{BasePath}/files{path}";
        }
    }
}
