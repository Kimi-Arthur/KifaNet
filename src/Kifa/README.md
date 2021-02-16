Kifa Common Library
===

Assembly Name
---
**Kifa.dll**

APIs
---
- `Kifa.Retry`
  - `Retry.Run(action, handleException)`
- `Kifa.ByteArrayExtensions`
  - `byte[].ToHexString()`
  - `long.ToByteArray()`
  - `byte[].ToInt64()`
- `Kifa.DictionaryExtensions`
  - `IDictionary.GetValueOrDefault(key, defaultValue)`
- `Kifa.MathExtensions`
  - `long.RoundUp(period)`
  - `int.RoundUp(period)`
  - `long.RoundDown(period)`
  - `int.RoundDown(period)`
- `Kifa.StringExtensions`
  - `string.Format(parameters)`
  - `string.Format(args)`
  - `string.ParseSizeString()`
  - `string.ParseHexString()`
  - `string.ParseTimeSpanString()`
- `Kifa.WebResponseExtensions`
  - `WebResponse.GetJToken()`
  - `WebResponse.GetObject()`
  - `WebResponse.GetDictionary()`
  - `HttpResponseMessage.GetJToken()`
  - `HttpResponseMessage.GetObject()`
