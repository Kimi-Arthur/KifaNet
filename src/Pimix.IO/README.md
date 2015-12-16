Pimix Input Output Library
===

Assembly Name
---
Pimix.IO.dll

Classes
---
 - `StorageClient` defines general client to manage files.

Current Version:
---
[![Nuget](https://img.shields.io/nuget/v/Pimix.IO.svg)](http://nuget.org/packages/Pimix.IO)

Dependencies:
---
 - **NewtonSoft.Json**: 7.0.1
 - **HashLib**: 2.0.1
 - **Pimix**: 1.0.3
 - **Pimix.Service**: 1.1.1

Changes:
---
 - 1.0.0
  - Begin package versioning.
 - 1.1.0
  - Added `Pimix.IO.StorageClient` interface.
 - 1.1.*
  - Use `Parallel.ForEach` to leverage multi processor's power when calculating hashes.
  - Use 3 digit version number now.
  - Fixed StorageClient.Exists's parameter.
  - Fix 'id'.
  - Updated Pimix.Service to 1.3.0, which includes retry.
  (We don't add retry control in this package for now).