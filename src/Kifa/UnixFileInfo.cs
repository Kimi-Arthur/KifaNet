using System;
using System.Runtime.InteropServices;

namespace Kifa;

public static class UnixFileInfo {
    public static ulong? GetInode(string path) {
        try {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                return GetMacInode(path);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                return GetLinuxInode(path);
            }
        } catch {
            // Ignored on unsupported platforms or missing native dependencies.
        }

        return null;
    }

    public static ulong? GetRefCount(string path) {
        try {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                return GetMacRefCount(path);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                return GetLinuxRefCount(path);
            }
        } catch {
            // Ignored on unsupported platforms or missing native dependencies.
        }

        return null;
    }

    // macOS (Darwin) stat64 implementation using libc.
    // Layout of struct stat64 from Darwin sys/stat.h:
    // https://github.com/apple-oss-distributions/xnu/blob/main/bsd/sys/stat.h
    [StructLayout(LayoutKind.Explicit, Size = 144)]
    struct StatMacBuf {
        [FieldOffset(0)] public int st_dev;
        [FieldOffset(4)] public ushort st_mode;
        [FieldOffset(6)] public ushort st_nlink;
        [FieldOffset(8)] public ulong st_ino;
    }

    [DllImport("libc", EntryPoint = "stat64", SetLastError = true)]
    static extern int StatMac(string path, out StatMacBuf buf);

    static ulong? GetMacInode(string path) {
        return StatMac(path, out var buf) == 0 ? buf.st_ino : null;
    }

    static ulong? GetMacRefCount(string path) {
        return StatMac(path, out var buf) == 0 ? (ulong) buf.st_nlink : null;
    }

    // Linux & Android statx implementation (kernel 4.11+).
    // Definitions for AT_FDCWD (-100), STATX_INO (0x100), and STATX_NLINK (0x4):
    // https://man7.org/linux/man-pages/man2/statx.2.html
    // https://github.com/torvalds/linux/blob/master/include/uapi/linux/fcntl.h
    // https://github.com/torvalds/linux/blob/master/include/uapi/linux/stat.h
    const int AtFdcwd = -100;
    const uint StatxIno = 0x00000100U;
    const uint StatxNlink = 0x00000004U;

    [StructLayout(LayoutKind.Explicit, Size = 256)]
    struct StatxBuf {
        [FieldOffset(0)] public uint stx_mask;
        [FieldOffset(16)] public uint stx_nlink;
        [FieldOffset(32)] public ulong stx_ino;
    }

    [DllImport("libc", EntryPoint = "statx", SetLastError = true)]
    static extern int Statx(int dirfd, string pathname, int flags, uint mask, out StatxBuf statxbuf);

    // Linux SYS_statx syscall numbers per architecture:
    // arm64/aarch64 (291), riscv64 (291): asm-generic unistd.h
    // x86_64 (332): arch/x86/entry/syscalls/syscall_64.tbl
    // arm (397): arch/arm/tools/syscall.tbl
    // x86 (383): arch/x86/entry/syscalls/syscall_32.tbl
    // Reference: https://marcin.juszkiewicz.com.pl/download/tables/syscalls.html
    static readonly IntPtr SysStatx = RuntimeInformation.ProcessArchitecture switch {
        Architecture.Arm64 => new IntPtr(291),
        Architecture.X64 => new IntPtr(332),
        Architecture.Arm => new IntPtr(397),
        Architecture.X86 => new IntPtr(383),
        _ => new IntPtr(291)
    };

    [DllImport("libc", EntryPoint = "syscall", SetLastError = true)]
    static extern IntPtr SyscallStatx(IntPtr number, int dirfd, string pathname, int flags, uint mask, out StatxBuf statxbuf);

    // Fallback standard stat on Linux 64-bit (x86_64, aarch64, and Android Bionic):
    // https://man7.org/linux/man-pages/man2/stat.2.html
    // https://android.googlesource.com/platform/bionic/+/refs/heads/master/libc/include/sys/stat.h
    [StructLayout(LayoutKind.Explicit, Size = 144)]
    struct StatLinuxBuf {
        [FieldOffset(0)] public ulong st_dev;
        [FieldOffset(8)] public ulong st_ino;
        [FieldOffset(16)] public uint st_mode;
        [FieldOffset(20)] public ulong st_nlink;
    }

    [DllImport("libc", EntryPoint = "stat", SetLastError = true)]
    static extern int StatLinux(string path, out StatLinuxBuf buf);

    static ulong? GetLinuxInode(string path) {
        try {
            if (Statx(AtFdcwd, path, 0, StatxIno, out var buf) == 0) {
                return (buf.stx_mask & StatxIno) != 0 ? buf.stx_ino : null;
            }
        } catch (EntryPointNotFoundException) {
            try {
                if (SyscallStatx(SysStatx, AtFdcwd, path, 0, StatxIno, out var buf) == IntPtr.Zero) {
                    return (buf.stx_mask & StatxIno) != 0 ? buf.stx_ino : null;
                }
            } catch (EntryPointNotFoundException) {
                if (StatLinux(path, out var buf) == 0) {
                    return buf.st_ino;
                }
            }
        }

        return null;
    }

    static ulong? GetLinuxRefCount(string path) {
        try {
            if (Statx(AtFdcwd, path, 0, StatxNlink, out var buf) == 0) {
                return (buf.stx_mask & StatxNlink) != 0 ? (ulong) buf.stx_nlink : null;
            }
        } catch (EntryPointNotFoundException) {
            try {
                if (SyscallStatx(SysStatx, AtFdcwd, path, 0, StatxNlink, out var buf) == IntPtr.Zero) {
                    return (buf.stx_mask & StatxNlink) != 0 ? (ulong) buf.stx_nlink : null;
                }
            } catch (EntryPointNotFoundException) {
                if (StatLinux(path, out var buf) == 0) {
                    return buf.st_nlink;
                }
            }
        }

        return null;
    }
}
