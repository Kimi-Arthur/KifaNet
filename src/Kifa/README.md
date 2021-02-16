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
