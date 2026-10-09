[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$archive = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../../art-source/portraits/conqueror-balanced'))
$manifest = Get-Content -LiteralPath (Join-Path $archive 'manifest.json') -Raw | ConvertFrom-Json
$records = @($manifest.Portraits)
if ($records.Count -ne 60) { throw 'Expected 60 basic portraits.' }
$targetRoot = Join-Path $PSScriptRoot 'conqueror-creation/tiles'
$backupRoot = Join-Path $archive 'pre-balanced-game-tiles'
foreach ($id in 1..60) {
    $record = @($records | Where-Object { $_.CharacterId -eq $id -and $_.Variant -eq 1 })
    if ($record.Count -ne 1) { throw "Missing or duplicated portrait $id" }
    $source = Join-Path $archive $record[0].GameTile
    if ((Get-FileHash -LiteralPath $source).Hash -ne $record[0].GameTileSha256) { throw "Tile hash mismatch: $id" }
}
[IO.Directory]::CreateDirectory($backupRoot) | Out-Null
foreach ($record in $records) {
    $name = 'character-{0:00}-1.png' -f $record.CharacterId
    $target = Join-Path $targetRoot $name
    $backup = Join-Path $backupRoot $name
    if ((Test-Path -LiteralPath $target) -and !(Test-Path -LiteralPath $backup)) {
        Copy-Item -LiteralPath $target -Destination $backup
    }
    Copy-Item -LiteralPath (Join-Path $archive $record.GameTile) -Destination $target -Force
}
$utf8 = [Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'conqueror-creation/selected-basic-versions.json'), ($records | ConvertTo-Json -Depth 5), $utf8)
'PASS: 60 balanced basic portraits exported to conquest creation; previous tiles archived; variants 2-6 and domestic portraits preserved.'
