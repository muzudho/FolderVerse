# Source artwork and Git LFS

`portraits/characters/` contains the portrait editing archive: generated originals,
versioned edits, catalog, prompts and previews. Runtime portraits remain in
`FolderVerse/Content/Images/Portraits/v*/` (including the selectable older editions).

The scripts in `FolderVerse/Content/Images/Portraits/` read this archive and export
the current 360 portraits into `v3-standardized/tiles/`. Their command locations
are unchanged. Catalog file paths remain relative to the characters directory.

Image, audio, video and listed art formats are tracked with Git LFS through the
root `.gitattributes`; text, JSON and scripts remain ordinary Git files.
After cloning, install Git LFS, run `git lfs install`, then `git lfs pull` before
building or editing. Build outputs remain ignored.

This setup converts the current index, not earlier Git history. Older commits
still contain ordinary Git image blobs. No commit or remote push is performed
by this configuration change.

## Backup and retention

Run `./scripts/backup-folderverse.ps1` from PowerShell. It saves a dated ZIP and
SHA256 sidecar directly in `E:\MuzudhoBackupsHDD` without nesting directories.
The archive includes working files, uncommitted changes, `.git/index`, Git history
and local LFS objects; build outputs and editor caches are excluded. A disconnected
HDD causes an explicit failure. Existing backups are never overwritten or deleted.
Avoid editing or running Git concurrently with a backup.

LFS stores complete versions, not binary deltas. Identical content reuses its
content hash; changed content requires another complete object. Removing a file
or rewriting history does not free GitHub's remote LFS storage. GitHub documents
repository deletion/recreation or contacting Support to purge removed objects:
https://docs.github.com/en/repositories/working-with-files/managing-large-files/removing-files-from-git-large-file-storage

`git lfs prune` only cleans the local cache, not GitHub storage. Keep dated HDD
backups before any retention changes. Do not automatically delete remote history
or artwork. Check account usage and the LFS spending budget before the first push.
