Pimix Common Library
===

Assembly Name
---
**Pimix.dll**

APIs
---
- `Pimix.Retry`
  - `Retry.Run(action, handleException)`
- `Pimix.ByteArrayExtensions`
  - `byte[].ToHexString()`
  - `long.ToByteArray()`
  - `byte[].ToInt64()`
- `Pimix.DictionaryExtensions`
  - `IDictionary.GetValueOrDefault(key, defaultValue)`
- `Pimix.MathExtensions`
  - `long.RoundUp(period)`
  - `int.RoundUp(period)`
  - `long.RoundDown(period)`
  - `int.RoundDown(period)`
- `Pimix.StringExtensions`
  - `string.Format(parameters)`
  - `string.Format(args)`
  - `string.ParseSizeString()`
  - `string.ParseHexString()`
  - `string.ParseTimeSpanString()`
- `Pimix.WebResponseExtensions`
  - `WebResponse.GetJToken()`
  - `WebResponse.GetObject()`
  - `WebResponse.GetDictionary()`
  - `HttpResponseMessage.GetJToken()`
  - `HttpResponseMessage.GetObject()`

Current Version:
---
[![Nuget](https://img.shields.io/nuget/v/Pimix.svg)](http://nuget.org/packages/Pimix)

Dependencies:
---
- **NewtonSoft.Json**: 11.0.2
- **NLog**: 4.5.10

Updates:
---

### 2.0
- Add `Retry` to handle generic retry logic.
- 2.1
  - Trace string output in `WebResponse.GetXxx`.

### 1.0
- Add extensions.
