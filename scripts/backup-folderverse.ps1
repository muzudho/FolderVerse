[CmdletBinding()]
param([string]$Destination = 'E:\MuzudhoBackupsHDD')
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (!(Test-Path -LiteralPath $Destination -PathType Container)) {
    throw "Backup HDD unavailable: $Destination"
}
$stamp = [DateTimeOffset]::UtcNow.ToOffset([TimeSpan]::FromHours(9)).ToString('yyyyMMdd-HHmmss-fff')
$archive = Join-Path $Destination "FolderVerse-$stamp.zip"
if (Test-Path -LiteralPath $archive) { throw 'Backup already exists.' }
# Include working files, staging index, Git history and local LFS objects.
# Reproducible build output and editor caches are excluded.
& tar -a -cf $archive --exclude='bin' --exclude='obj' --exclude='.vs' --exclude='.version-lock' -C $root .
if ($LASTEXITCODE -ne 0) { throw "Archive creation failed: $archive" }
$entries = @(& tar -tf $archive)
if ($LASTEXITCODE -ne 0 -or !($entries -contains './.git/HEAD') -or !($entries -contains './.gitattributes')) {
    throw "Archive verification failed: $archive"
}
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash
[IO.File]::WriteAllText($archive + '.sha256', "$hash  $([IO.Path]::GetFileName($archive))`r`n", [Text.UTF8Encoding]::new($false))
Get-Item -LiteralPath $archive | Select-Object FullName, Length
