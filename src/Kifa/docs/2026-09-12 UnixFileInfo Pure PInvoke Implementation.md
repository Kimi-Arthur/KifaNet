# UnixFileInfo Pure P/Invoke Implementation

## Overview
[`UnixFileInfo`](file:///Users/jingbian/Projects/KifaNet/src/Kifa/UnixFileInfo.cs) retrieves Unix-specific filesystem metadata—specifically file **inode numbers** (`st_ino`) and **hard link counts** (`st_nlink`)—for local files. These are used across KifaNet storage clients ([`FileStorageClient`](file:///Users/jingbian/Projects/KifaNet/src/Kifa.IO/StorageClients/FileStorageClient.cs)) for rapid file deduplication and file identity verification without reading entire file contents from disk.

---

## Why `Mono.Unix` Was Replaced

Previously, [`UnixFileInfo`](file:///Users/jingbian/Projects/KifaNet/src/Kifa/UnixFileInfo.cs) relied on the third-party `Mono.Unix` NuGet package (`Mono.Unix.Native.Syscall.stat`).

### Problems with `Mono.Unix`
1. **Missing Native Shims on Android (Termux) and Non-Standard Environments**:
   `Mono.Unix` does not P/Invoke directly to standard C runtime libraries (`libc`). Instead, it relies on a bundled native helper library (`libMonoPosixHelper.so` / `Mono.Unix.so`). On Android (Bionic `libc`), Termux, and musl-based distributions where this native shim is not packaged, referencing `Syscall` threw:
   ```
   System.TypeInitializationException: The type initializer for 'Mono.Unix.Native.Syscall' threw an exception.
   ---> System.DllNotFoundException: Mono.Unix
   ```
2. **Process Crashes**:
   Because `UnixFileInfo` called `Syscall.stat` without global exception insulation, the unhandled `TypeInitializationException` crashed file operations such as `UploadCommand` during file identity resolution.
3. **Legacy Dependency**:
   `Mono.Unix` is unmaintained and carries redundant native dependencies for simple syscalls that are natively supported by modern OS kernels.

---

## Pure P/Invoke Architecture

[`UnixFileInfo`](file:///Users/jingbian/Projects/KifaNet/src/Kifa/UnixFileInfo.cs) directly invokes standard C library (`libc`) system calls, with platform-specific handling and graceful degradation.

### 1. macOS (Darwin)
On macOS (64-bit and Apple Silicon):
- **Entry Point**: `libc.stat64` in `libSystem.dylib` (or `libc`).
- **Structure Layout**: 144-byte `struct stat64` defined in Darwin [`bsd/sys/stat.h`](https://github.com/apple-oss-distributions/xnu/blob/main/bsd/sys/stat.h):
  - `st_dev` (`int32`, offset 0)
  - `st_mode` (`uint16`, offset 4)
  - `st_nlink` (`uint16`, offset 6)
  - `st_ino` (`uint64`, offset 8)

### 2. Linux & Android (Termux)
On Linux kernels 4.11+ (including Android 8+):
- **Primary Mechanism**: Modern [`statx(2)`](https://man7.org/linux/man-pages/man2/statx.2.html) system call via `libc.statx`.
  - Flags: `AT_FDCWD` (`-100`), requesting `STATX_INO` (`0x100`) and `STATX_NLINK` (`0x4`).
  - `struct statx`: Architecture-independent 256-byte structure (`stx_nlink` at offset 16, `stx_ino` at offset 32).
- **Direct Syscall Fallback (`SYS_statx`)**:
  If `libc.statx` is not exported as a C library wrapper in older Bionic or glibc versions, `syscall(SYS_statx, ...)` is called using architecture-specific syscall numbers:
  - ARM64 (`aarch64`): `291`
  - x86_64: `332`
  - ARM 32-bit: `397`
  - x86 32-bit: `383`
- **Classic `stat(2)` Fallback**:
  If `statx` is not available, falls back to 64-bit `stat` via `libc.stat`.

---

## Error Insulation & Graceful Degradation
- All platform invocations in `UnixFileInfo.GetInode` and `UnixFileInfo.GetRefCount` are enclosed within `try ... catch` guards.
- If a native library, entry point, or platform is unsupported (e.g. Windows, non-Unix platforms, or unsupported filesystems), the methods safely return `null`.
- When `GetInode` returns `null`, callers (such as `FileStorageClient.GetFileIdInfo`) gracefully fall back to checksum-based verification without interrupting operations.
