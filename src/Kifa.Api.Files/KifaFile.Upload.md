# Logic flow for KifaFile.Upload.

## Parameters

- `this` (`KifaFile`): source file
- `targets` (`List<CloudTarget>`): types of cloud targets to upload to
- `deleteSource` (`bool`): whether to delete source after successful upload
- `useCache` (`bool`): whether to cache file to local to avoid multiple access
- `downloadLocal` (`bool`): whether to keep the local version
- `skipVerify` (`bool`): whether to skip verifying the upload result
- `skipRegistered` (`bool`): whether to skip uploading files that are previously uploaded (and verified)

## Returns

`List<(CloudTarget target, string? destination, bool? result)>`

Each element represents the result of uploading to one target
- `true`: success
- `false`: failure
- `null`: skipped as requested

## Main Steps

```puml
start
:Check source;
group For every file
  :Get destination;
  :Upload to destination;
  :Check destination;
end group
:Delete Source;
stop
```
