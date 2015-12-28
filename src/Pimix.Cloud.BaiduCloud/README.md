Pimix Baidu Cloud Library
===

Assembly Name
---
Pimix.Cloud.BaiduCloud.dll

Classes
---
 - `BaiduCloudStorageClient` implements `StorageClient` for accessing storage service in pan.baidu.com.

Current Version:
---
[![Nuget](https://img.shields.io/nuget/v/Pimix.Cloud.BaiduCloud.svg)](http://nuget.org/packages/Pimix.Cloud.BaiduCloud)

Dependencies:
---
 - **NewtonSoft.Json**: 7.0.1
 - **Pimix**: 1.0.3
 - **Pimix.IO**: 1.1.4
 - **Pimix.Service**: 1.1.1

Changes:
---
 - 1.0.0
  - Begin package versioning.
 - 1.0.*
  - Update method names due to upstream update.
 - 1.1.0
  - Implemented Pimix.Cloud.BaiduCloud.BaiduCloudStorageClient.Exists.
 - 1.1.*
  - Fix configs name.
  - Fix 'id'.
  - Updated Pimix.Service to 1.3.0, which includes retry.
    (We don't add retry control in this package for now).
  - Updated Pimix.IO to 1.1.5.
 - 1.2.0
  - Added per-block upload verification.
  - Better logging for downloading exceptions.
