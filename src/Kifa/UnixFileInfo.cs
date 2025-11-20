namespace Kifa;

using Mono.Unix.Native;

// Use Mono.Posix version when https://github.com/mono/mono.posix/issues/51 is resolved.
public class UnixFileInfo {
    public static ulong? GetInode(string path) {
        return Syscall.stat(path, out var stat) == 0 ? stat.st_ino : null;
    }

    public static ulong? GetRefCount(string path) {
        return Syscall.stat(path, out var stat) == 0 ? stat.st_nlink : null;
    }
}
