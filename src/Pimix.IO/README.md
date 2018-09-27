Pimix Input Output Library
===

Assembly Name
---
**Pimix.IO.dll**

APIs
---
- `Pimix.IO.FileInformation`
  - Information about a file.
- `Pimix.IO.StorageClient`
  - Interface to manage files.
- `Pimix.IO.FileStorageClient`
  - A `StorageClient` to manage simple files supported by `File`.
- `Pimix.IO.VerifiableStream`
  - A `Stream` that uses `FileInformation` to verifiably read data.
    - It will throw `Exception` if it's still incorrect after 5 tries.
    - If `FileInformation` is not provided, it will read 2 to 5 times to reach a majority agreement
      to make it verifiable.
- `Pimix.IO.SeekableReadStream`
  - A `Stream` wrapping over a function that can read data based on arbitrary position and count.
 
Current Version:
---
[![Nuget](https://img.shields.io/nuget/v/Pimix.IO.svg)](http://nuget.org/packages/Pimix.IO)

Dependencies:
---
 - **NLog**: 4.5.10
 - **NStratis.HashLib**: 1.0.0.1
 - **SSH.NET**: 2016.1.0
 - **Pimix.Service**

Updates:
---
### 2.0
- Added `VerifiableStream`.
- 2.1
  - Added `SeekableReadStream`.

### 1.0
- Added `FileInformation`.
- 1.1
  - Added `Pimix.IO.StorageClient` interface.
- 1.2
  - Add `FileStorageClient`.

Pending Tasks:
---

- [ ] **`VerifiableStream`** Log to `Trace` when appropriate.
- [ ] **`VerifiableStream`** Throw proper `Exception` if not verified.
