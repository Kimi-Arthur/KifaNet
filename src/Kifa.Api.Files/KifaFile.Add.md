# KifaFile.Add()

This method adds a `KifaFile` (a concrete file instance) as a `Location` of a `FileInformation` object.

## Arguments

- `shouldCheckKnown`: whether the file instance should be recheck if it's already in the system.
    - `null` (default): only a quick check (only `Size` is verified, which will make sure the file actually exists) is
      done.
    - `true`: should always be checked.
    - `false`: should never be checked.

## Control Flow

### Quick mode

- If file is registered and
    - if `shouldCheckKnown` is `false` => Skip.
    - if `shouldCheckKnown` is `null` => Check `Size` and skip or fail.
    - if `shouldCheckKnown` is `true` => Continue to the following situations.
- Otherwise, continue to the following situations.

### Situation 0: `FileIdInfo` is found.

This means the exact copy is checked at one point. So probably don't need to check if things are right.

A `FileInformation` should exist (found by `SHA256`).

Since `FileIdInfo` is not a strong indication, any failure will just be warned and continue to the other two situations.

- `Size` and `LastModified` not match => Ignore.
- `FileInfo` exists
    - If `Sha256` match, Register().
    - Otherwise, ignore.
- Otherwise, if the one linked by `Sha256`
    - If `Sha256` match (supposedly always), link `Id` to `Sha256` item and Register().
    - Otherwise, ignore.

### Situation 1: FileInfo doesn't exist (TBC)

1. Calculate full info of the file.
2. Find the `SHA256` `FileInformation` and compare. Fail if they differ.
3. Register.

### Situation 2: FileInfo exists (TBC)

1. Calculate full info of the file.
2. Find the `SHA256` `FileInformation` and compare. Fail if they differ.
3. Register.

### Info check precedence

In case a full check is needed, either due to explicit request or inference (file info not calculated or
instance not known), its full info will be calculated and compared to existing ones if possible. This can ensure
anything registered is guaranteed to be the same file.

If the file path is known, it's first compared with that info.
Then if a file with the same SHA256 is found, compare with that info too.