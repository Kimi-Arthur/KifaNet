Kifax Input Output Library
===

Assembly Name
---
**Kifax.IO.dll**

APIs
---
- `Kifax.IO.FileInformation`
  - Information about a file.
- `Kifax.IO.StorageClient`
  - Interface to manage files.
- `Kifax.IO.FileStorageClient`
  - A `StorageClient` to manage simple files supported by `File`.
- `Kifax.IO.VerifiableStream`
  - A `Stream` that uses `FileInformation` to verifiably read data.
    - It will throw `Exception` if it's still incorrect after 5 tries.
    - If `FileInformation` is not provided, it will read 2 to 5 times to reach a majority agreement
      to make it verifiable.
- `Kifax.IO.SeekableReadStream`
  - A `Stream` wrapping over a function that can read data based on arbitrary position and count.

Pending Tasks:
---

- [ ] **`VerifiableStream`** Log to `Trace` when appropriate.
- [ ] **`VerifiableStream`** Throw proper `Exception` if not verified.
