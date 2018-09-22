Pimix Input Output Library
===

Assembly Name
---
`Pimix.IO.dll`

Features
---
- `Pimix.IO.StorageClient` defines base client to manage files.
- `Pimix.IO.FileStorageClient` implements `StorageClient` to manage simple files supported by `File`.

Current Version:
---
[![Nuget](https://img.shields.io/nuget/v/Pimix.IO.svg)](http://nuget.org/packages/Pimix.IO)

Dependencies:
---
 - **NLog**: 4.5.9
 - **NStratis.HashLib**: 1.0.0.1
 - **SSH.NET**: 2016.0.0
 - **Pimix.Service**: csproj

Updates:
---
###1.2
- Add `FileStorageClient`.
- 1.2.1
  - Fix `FileStorageClient` accessibility.
- 1.2.2
  - Update dependency logic.- 1.0.0
  - Begin package versioning.

###1.1
- Added `Pimix.IO.StorageClient` interface.
- 1.1.1
  - Use `Parallel.ForEach` to leverage multi processor's power when calculating hashes.
  - Use 3 digit version number now.
- 1.1.2
  - Fixed StorageClient.Exists's parameter.
  - Fix 'id'.
  - Updated Pimix.Service to 1.3.0, which includes retry.
    (We don't add retry control in this package for now).

Pending Tasks:
---

- [ ] Make `VerifiableStream` log to `Trace` when appropriate.