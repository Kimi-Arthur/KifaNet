using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pimix.IO {
    public class FileStorageClient : StorageClient {
        const int DefaultBlockSize = 32 << 20;

        public static Dictionary<string, string> PathMap = new Dictionary<string, string>();

        public static StorageClient Get(string fileSpec) {
            var specs = fileSpec.Split(';');
            foreach (var spec in specs)
                if (spec.StartsWith("local:"))
                    return new FileStorageClient {
                        BaseId = spec.Substring(6),
                        BasePath = PathMap.GetValueOrDefault(spec.Substring(6), null)
                    };

            return null;
        }

        string BaseId { get; set; }

        string BasePath { get; set; }

        public override string ToString() => $"local:{BaseId}";

        public override void Copy(string sourcePath, string destinationPath)
            => File.Copy(GetPath(sourcePath), GetPath(destinationPath));

        public override void Delete(string path) => File.Delete(GetPath(path));

        public override void Move(string sourcePath, string destinationPath)
            => File.Move(GetPath(sourcePath), GetPath(destinationPath));

        public override bool Exists(string path) => File.Exists(GetPath(path));

        public override IEnumerable<FileInformation> List(string path, bool recursive = false) {
            var normalizedPath = GetPath(path);
            if (!Directory.Exists(normalizedPath)) {
                return Enumerable.Empty<FileInformation>();
            }

            var directory = new DirectoryInfo(normalizedPath);
            var items = directory.GetFiles("*",
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            return items.OrderBy(i => i.Name).Select(i => new FileInformation() {
                Id = GetId(i.FullName),
                Size = i.Length
            });
        }

        public override Stream OpenRead(string path) => File.OpenRead(GetPath(path));

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

        string GetId(string path) => path.Substring(BasePath.Length).Replace("\\", "/");

        public override string GetPath(string path) => $"{BasePath}{path}";
    }
}
