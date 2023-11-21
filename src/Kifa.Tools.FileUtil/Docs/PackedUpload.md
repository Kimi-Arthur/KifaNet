# Packed Upload

## Problem

When uploading a large number of small files, overhead of each file can be a big issue. For tele: targets, it takes 4.5
seconds upload a file of 100 KB (22.2KB/s), while it takes 7 minutes to upload a file of size 1.41GB (3.3MB/s).
Downloading has a similar pattern with 3.6s (27.7KB/s) and 1min32s (15.2MB/s). So it's about 150 times and > 500 times.

## Idea

Packing this kind of files together can significantly reduce time for both uploading and downloading.

### Pack File Naming

Currently all uploaded files are named as `/$/<SHA256>.<version>.<part>`, where `<SHA256>` is unique for each file so no
collide is possible, while each file can be verified without help of where it's originally located.

To avoid collide with existing hashed file path, we can use a new folder `/#/` and XOR'ed SHA256 of all files contained
like `/#/<SHA256>.vp` with possible `<part>` suffix. This way it's only content dependent and won't be affected by in
renames and/or reordering of the files.

### Pack File Format

The packed format will have no actual format inside, but instead concatenated blocks for each contained file. This is to
reduce unnecessary encryption etc. It's kind of like the sharded file format where it's only meaningful when all shards
are concatenated.

There will be a new `PackFileFormat` or `PackFileStorageClient` to handle this layer of packing, which should support
caching of access info so that they can be reused when downloading all the files in the pack. It should also support
access to single files alone.

#### Split File: No special handling beyond what ShardedStorageClient is providing.

The packed file can get larger than cloud service's limit. For some services there will be fragmentation if the sizes
are close to a certain threshold. Our strategy would be not manipulate that on this level, but treat it as a big file
and let cloud file service clients handle them.

The only caveat is that a small file may occupy two parts, which will double its access time, but compared to the number
of files that reduce the access time and simplicity of the system, this is well worth it.

### Pack File Data

It will be a normal file like `tele:kimily/#/<SHA256>.vp`, but no entry like `/#/<SHA256>.vp` exists. A multi-part file
will look like `tele:kimily*3/#/<SHA256>.vp`, with actual part file being `tele:kimily/#/<SHA256>.vp.1`.

We can redefine meanings of all the fields to be a container of data contained:

- public long? Size { get; set; }
  - Total length of packed files combined. (is that internal format dependent then?)
- public List<string> BlockSha256 { get; set; }
  - Sha256 of each contained files

- public SortedDictionary<string, DateTime?> Locations { get; set; } = new();
- Ignored fields
  - public string? Md5 { get; set; }
  - public string? Sha1 { get; set; }
  - public string? Sha256 { get; set; }
  - public string? Crc32 { get; set; }
  - public string? Adler32 { get; set; }
  - public List<string> BlockMd5 { get; set; }
  - public List<string> BlockSha1 { get; set; }
  - public string? SliceMd5 { get; set; }
  - public string? EncryptionKey { get; set; }


### Original File Data

It will be a new type of location key. It will look like `tele:kimily/#/<SHA256>:32767:33000.v2`, or
`tele:kimily/#/<SHA256>.vp:32767:33000.v2`, meaning the encrypted content will be the bytes `[32767, 33000)`
of file `tele:kimily/#/<SHA256>.vp` of `v2` format.

### Uploading

`filex` will support a new option called `-p` which will pack all files that are not already uploaded. It will treat all
inputs as one file.

### Downloading (Phase 2)

It will be the same when downloading, no matter it's one file in the pack or the whole pack. Some optimization may be
needed to cache pack file entry info when downloading a lot of files in pack.

As long as we upload in the order intended to be downloaded, we don't need to consider much about downloading for now.
Even if the order is correct, we can still optimize the downloading logic later for that.

### Deletion (TBC)

For now, deleting packs can be difficult as well as actually deleting files in the pack.
