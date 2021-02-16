Kifax Common Library
===

Assembly Name
---
**Kifax.dll**

APIs
---
- `Kifax.Retry`
  - `Retry.Run(action, handleException)`
- `Kifax.ByteArrayExtensions`
  - `byte[].ToHexString()`
  - `long.ToByteArray()`
  - `byte[].ToInt64()`
- `Kifax.DictionaryExtensions`
  - `IDictionary.GetValueOrDefault(key, defaultValue)`
- `Kifax.MathExtensions`
  - `long.RoundUp(period)`
  - `int.RoundUp(period)`
  - `long.RoundDown(period)`
  - `int.RoundDown(period)`
- `Kifax.StringExtensions`
  - `string.Format(parameters)`
  - `string.Format(args)`
  - `string.ParseSizeString()`
  - `string.ParseHexString()`
  - `string.ParseTimeSpanString()`
- `Kifax.WebResponseExtensions`
  - `WebResponse.GetJToken()`
  - `WebResponse.GetObject()`
  - `WebResponse.GetDictionary()`
  - `HttpResponseMessage.GetJToken()`
  - `HttpResponseMessage.GetObject()`
