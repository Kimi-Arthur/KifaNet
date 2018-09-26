Pimix Service Library
===

Assembly Name
---
**Pimix.Service.dll**

Configurable properties
---
- `Pimix.Service.PimixService`
  - `PimixServerApiAddress`
  - `PimixServerCredential`

APIs
---
- `Pimix.Service.PimixService`
  - `Patch`
  - `Post`
  - `Get`
  - `Link`
  - `Delete`
  - `Call`

Current Version:
---
[![Nuget](https://img.shields.io/nuget/v/Pimix.Service.svg)](http://nuget.org/packages/Pimix.Service)

Dependencies:
---
 - **Pimix**

Updates:
---

### 2.0
- Use `Pimix.Retry` to do retry.

### 1.0
- Add standard ActionView's Call support.
- 1.1
  - Add README.md etc.
- 1.2
  - Add retry support for `PimixService`.
  - Print exception info in retrying.
  - Longer wait period for retrying.
- 1.3
  - New correct way to implement retry logic in PimixService.
- 1.4
  - New (status + message + response) format contacting PimixServer.
  - Update dependency logic.