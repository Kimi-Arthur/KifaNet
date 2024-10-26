# Multi User Support

We want to be able to separate access for different users. This includes multiple degrees and aspects.

## 0. Get the user making the request

We use Client Cert Auth for both browser users and CLI users. For browser usage, the browser will prompt with selection
of certs. For CLI, an alias should be created to take advantage of the `--config` flag.

So we would need different certs for different users as well as different configs. It would be reasonable to change the
current config structure as different users share most of the configs while have some minor differences. (Maybe a chain
of configs like `*.rc` may be reasonable)

## 1. Limit access to different API / DataModel

Use `UserFilter` to accept / reject requests based on incoming userid.

One thing we found out later is that only limiting access to the top level controllers is not enough, as it may access
other data when fulfilling the requests. To solve that, we delay the access checking to `KifaServiceJsonClient` while
`UserFilter` only assigns configs for the detected user.

## 2. Use separate data storages for different users

To make one `Kifa.Web.Api` service act differently for different users for the same type of service, we can make
`KifaServiceJsonClient` to load data from different folders.

One thing to note is also related to the thing above. As the implementation of one service may need to access another
one, the folders assigned to the user should apply to all services that may be needed.

## 3. Cleanups

There are several cleanups needed to finish the work. Some infra work needed:

1. Support auto populate and maybe auto delete of virtually linked items.
2. TBA