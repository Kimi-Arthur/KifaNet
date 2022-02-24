# myCloud API

## Auth

Login and requests will have a `Authorization` header, which contains the access_token.

Example: `eyfL694sQruQqjOJ8L3G8g==`

Put as Bearer token, like:

```
Authorization: Bearer eyfL694sQruQqjOJ8L3G8g==
```

The token is also available in cookie named `mycloud-login_token`. It's url encoded:

```
{"userName":"tokenUser","access_token":"nfOZ3r7ZSgm7zxZaXScB8g==","token_type":"Bearer",".expires":"2019-11-18T13:54:16.464Z"}
```

## Object/Path id generation

Base64 encode the full path, like: `/Drive/灵笼-22088/《灵笼：INCARNATION》定档PV [PV4]定档PV-av47419190.c83279233.mp4`.

```http request
GET https://storage.prod.mdl.swisscom.ch/metadata?p=L0RyaXZlL-eBteesvC0yMjA4OC_jgIrngbXnrLzvvJpJTkNBUk5BVElPTuOAi-Wumuaho1BWIFtQVjRd5a6a5qGjUFYtYXY0NzQxOTE5MC5jODMyNzkyMzMubXA0
Authorization: Bearer eyfL694sQruQqjOJ8L3G8g==
```

## References

https://github.com/thomasgassmann/mycloud-cli
