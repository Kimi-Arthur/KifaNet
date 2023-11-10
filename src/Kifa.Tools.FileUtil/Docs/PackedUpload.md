# Packed Upload

## Problem

When uploading a large number of small files, overhead of each file can be a big issue. For tele: targets, it takes 4.5
seconds upload a file of 100 KB (22.2KB/s), while it takes 7 minutes to upload a file of size 1.41GB (3.3MB/s).
Downloading has a similar pattern with 10s (10KB/s) and 2min28ss (9.5MB/s). So it's about 150 times and 1000 times.

## Idea

Packing this kind of files together can significantly reduce time for both uploading and downloading.

### Pack File Naming

Currently all uploaded files are named as `/$/<SHA256>.<version>.<part>`, where `<SHA256>` is unique for each file so no
collide is possible, while each file can be verified without help of where it's originally located.

To avoid collide with existing hashed file path, we can use a new folder `/#/` and timestamp based file name like
`/#/20231109222436000000` with possible `<part>` suffix.

### Pack File Format

The packed format will have no actual format inside, but instead concatenated blocks for each contained file. This is to
reduce unnecessary encryption etc.

There will be a new `PackFileFormat` or `PackFileStorageClient` to handle this layer of packing, which should support
caching of access info so that they can be reused when downloading all the files in the pack. It should also support
single access.

#### Split File

The packed file can get larger than cloud service's limit. For some services there will be fragmentation if the sizes
are close to a certain threshold. Our strategy would be not manipulate that on this level, but treat it as a big file
and let cloud file service clients handle them.

The only caveat is that a small file may occupy two parts, which will double its access time, but compared to the number
of files that reduce the access time and simplicity of the system, this is well worth it.

### Pack File Data

It will be a normal file residing in `/#/`, but the uploaded one will have no encryption. So a location key like
`tele:kimily/#/20231109222436000000` may exist, but should be considered as `IsCloud` in `KifaFile`. Maybe a new format
suffix like `vp` can be helpful too, like `tele:kimily*3/#/20231109222436000000.vp`, with actual part file being
`tele:kimily/#/20231109222436000000.vp.1`.

(TBC) A list of the contained files may be interesting to record somewhere, but it's mostly useless before we know how
to delete pack files properly.

### Original File Data

It will be a new type of location key. It will look like `tele:kimily/#/20231109222436000000:32767:33000.v2`, or
`tele:kimily/#/20231109222436000000.vp:32767:33000.v2`, meaning the encrypted content will be the bytes `[32767, 33000)`
of file `tele:kimily/#/20231109222436000000.vp` of `v2` format.

### Uploading

`filex` will support a new option called `-p` which will pack all files that are not already uploaded. It will treat all
inputs as one file.

### Downloading

It will be the same when downloading, no matter it's one file in the pack or the whole pack. Some optimization may be
needed to cache pack file entry info when downloading a lot of files in pack,

### Deletion (TBC)

For now, deleting packs can be difficult as well as actually deleting files in the pack. We will still keep a pack
object maybe so that we know what's inside.
