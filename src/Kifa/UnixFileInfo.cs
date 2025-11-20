namespace Kifa;

using Mono.Unix.Native;

public class UnixFileInfo {
    public static ulong? GetInode(string path) {
        return Syscall.stat(path, out var stat) == 0 ? stat.st_ino : 0;
    }
}
