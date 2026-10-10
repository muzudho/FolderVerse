[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$archive = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../../art-source/portraits/conqueror-balanced'))
$progress = Get-Content -LiteralPath (Join-Path $archive 'remaining-progress.json') -Raw | ConvertFrom-Json
$records = @($progress.Portraits)
if ($records.Count -ne $progress.Completed) { throw 'Progress count mismatch.' }
$keys = @{}
foreach ($record in $records) {
    $key = '{0}-{1}' -f $record.CharacterId,$record.Variant
    if ($keys.ContainsKey($key) -or !$record.Reviewed -or $record.Variant -lt 2 -or $record.Variant -gt 6) { throw "Invalid portrait: $key" }
    $keys[$key] = $true
    $source = Join-Path $archive $record.File
    if ((Get-FileHash -LiteralPath $source).Hash -ne $record.Sha256) { throw "Source hash mismatch: $key" }
}
$targetRoot = Join-Path $PSScriptRoot 'conqueror-creation/tiles'
$tileRoot = Join-Path $archive 'game-tiles'
$backupRoot = Join-Path $archive 'pre-balanced-game-tiles'
[IO.Directory]::CreateDirectory($backupRoot) | Out-Null
$selected = foreach ($record in $records) {
    $name = 'character-{0:00}-{1}.png' -f $record.CharacterId,$record.Variant
    $source = Join-Path $archive $record.File
    $target = Join-Path $targetRoot $name
    $backup = Join-Path $backupRoot $name
    $image = [Drawing.Bitmap]::FromFile($source)
    try {
        if ($image.Width * 3 -ne $image.Height * 4) { throw "Not 4:3: $name" }
        $tile = [Drawing.Bitmap]::new(256,192)
        $graphics = [Drawing.Graphics]::FromImage($tile)
        try {
            $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $graphics.DrawImage($image,[Drawing.Rectangle]::new(0,0,256,192))
            $tile.Save((Join-Path $tileRoot $name),[Drawing.Imaging.ImageFormat]::Png)
        } finally { $graphics.Dispose(); $tile.Dispose() }
    } finally { $image.Dispose() }
    if ((Test-Path -LiteralPath $target) -and !(Test-Path -LiteralPath $backup)) { Copy-Item -LiteralPath $target -Destination $backup }
    Copy-Item -LiteralPath (Join-Path $tileRoot $name) -Destination $target -Force
    $hash = (Get-FileHash -LiteralPath $target).Hash
    if ($hash -ne (Get-FileHash -LiteralPath (Join-Path $tileRoot $name)).Hash) { throw "Export hash mismatch: $name" }
    [ordered]@{ CharacterId=$record.CharacterId; Variant=$record.Variant; Source=$record.File; SourceSha256=$record.Sha256; GameTile=$name; GameTileSha256=$hash }
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'conqueror-creation/selected-remaining-versions.json'),($selected | ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))
"PASS: $($records.Count) reviewed variants exported; previous tiles archived."
