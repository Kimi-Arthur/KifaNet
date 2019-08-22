# myCloud API

## Auth
Login and requests will have a `Authorization` header, which contains the access_token.

Example: `eyfL694sQruQqjOJ8L3G8g==`

Put as Bearer token, like:

```
Authorization: Bearer eyfL694sQruQqjOJ8L3G8g==
```


## Object/Path id generation

Base64 encode the full path.

```http request
GET https://storage.prod.mdl.swisscom.ch/metadata?p=L0RyaXZlL-eBteesvC0yMjA4OC_jgIrngbXnrLzvvJpJTkNBUk5BVElPTuOAi-Wumuaho1BWIFtQVjRd5a6a5qGjUFYtYXY0NzQxOTE5MC5jODMyNzkyMzMubXA0
Authorization: Bearer eyfL694sQruQqjOJ8L3G8g==
```

## References
https://github.com/thomasgassmann/mycloud-cli