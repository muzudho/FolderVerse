[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$utf8 = [Text.UTF8Encoding]::new($false)
$sourceRoot = Join-Path $PSScriptRoot '../characters'
$sources = @(Get-Content -LiteralPath (Join-Path $sourceRoot 'catalog.json') -Raw | ConvertFrom-Json | Where-Object { $_.Current -and $_.Variant -eq 1 })
if ($sources.Count -ne 60) { throw 'Expected 60 current basic source portraits.' }
$tileRoot = Join-Path $PSScriptRoot 'game-tiles'
[IO.Directory]::CreateDirectory($tileRoot) | Out-Null
$records = @()
$overrides = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'selection-overrides.json') -Raw | ConvertFrom-Json
foreach ($id in 1..60) {
    $source = @($sources | Where-Object CharacterId -eq $id)
    if ($source.Count -ne 1) { throw "Missing or duplicated character $id" }
    $file = 'character-{0:00}-1-balanced.png' -f $id
    $revision = 'character-{0:00}-1-balanced-v2.png' -f $id
    if (Test-Path -LiteralPath (Join-Path $PSScriptRoot $revision)) { $file = $revision }
    $override = $overrides.PSObject.Properties[[string]$id]
    if ($null -ne $override) { $file = [string]$override.Value }
    $path = Join-Path $PSScriptRoot $file
    $image = [Drawing.Bitmap]::FromFile($path)
    try {
        if ($image.Width * 3 -ne $image.Height * 4) { throw "Not 4:3: $file" }
        $tileFile = 'character-{0:00}-1.png' -f $id
        $tile = [Drawing.Bitmap]::new(256,192)
        $graphics = [Drawing.Graphics]::FromImage($tile)
        try {
            $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $graphics.DrawImage($image, [Drawing.Rectangle]::new(0,0,256,192))
            $tile.Save((Join-Path $tileRoot $tileFile), [Drawing.Imaging.ImageFormat]::Png)
        } finally { $graphics.Dispose(); $tile.Dispose() }
        $records += [ordered]@{
            CharacterId = $id; Variant = 1; File = $file
            Width = $image.Width; Height = $image.Height
            Sha256 = (Get-FileHash -LiteralPath $path).Hash
            OriginalFile = '../characters/' + $source[0].OriginalFile
            OriginalSha256 = $source[0].OriginalSha256
            GameTile = 'game-tiles/' + $tileFile
            GameTileSha256 = (Get-FileHash -LiteralPath (Join-Path $tileRoot $tileFile)).Hash
            Reviewed = $true
        }
    } finally { $image.Dispose() }
}
$manifest = [ordered]@{
    Purpose = 'Conqueror creation basic portraits, 60 characters, variant 1 only'
    Created = '2026-10-10'; Generator = 'Built-in image_gen; individual identity-preserving edits'
    Balance = '../face-size-trials/portrait-balance.json'
    Prompt = '../face-size-trials/all-60-balance-prompt.txt'
    Ratios = 'Approximate visual targets, not measured geometric transformations'
    RuntimeAdoption = 'Basic portraits adopted in conquest creation; domestic affairs and variants 2-6 preserved'
    Portraits = $records
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'manifest.json'), ($manifest | ConvertTo-Json -Depth 6), $utf8)
$cards = foreach ($record in $records) {
    '<article><h2>{0:00}</h2><a href="{1}"><img loading="lazy" src="{1}" alt="キャラクター {0:00} 調整版"></a><p><a href="{1}">調整版の原寸</a> · <a href="{2}">元画像</a> · <a href="{3}">ゲーム用縮小版</a></p></article>' -f $record.CharacterId,$record.File,$record.OriginalFile,$record.GameTile
}
$html = @'
<!doctype html><html lang="ja"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>FolderVerse 女の子60人・基本画像</title>
<style>body{margin:24px;background:#141725;color:#f4f2fa;font-family:system-ui,sans-serif}h1{font-size:24px}header{margin-bottom:24px}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(320px,1fr));gap:20px}article{background:#24283b;border-radius:12px;padding:12px}h2{margin:0 0 8px;font-size:18px}img{width:100%;aspect-ratio:4/3;object-fit:contain;border-radius:6px}a{color:#b4d9ff}p{font-size:13px;line-height:1.7}</style>
<header><h1>FolderVerse 女の子60人・基本画像</h1><p>征服者作成用の調整版。顔・手・耳・瞳のハイライト・頭の飾りを共通のバランスで調整。構図はポーズごとに調整しています。</p><p>画像を押すと原寸で表示。元画像と内政フェーズ用は保存済み。ゲーム用縮小版は256×192。60人の基本画像を征服者作成に採用しました。</p></header><main>
'@
$html += ($cards -join "`n") + '</main></html>'
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'gallery.html'), $html, $utf8)
foreach ($page in 0..1) {
    $sheet = [Drawing.Bitmap]::new(1600,1584)
    $canvas = [Drawing.Graphics]::FromImage($sheet)
    $font = [Drawing.Font]::new('Segoe UI',14)
    try {
        $canvas.Clear([Drawing.Color]::FromArgb(20,23,37))
        $canvas.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        foreach ($slot in 0..29) {
            $record = $records[$page*30+$slot]
            $x = ($slot % 5)*320; $y = [int][Math]::Floor($slot/5)*264
            $portrait = [Drawing.Bitmap]::FromFile((Join-Path $PSScriptRoot $record.File))
            try { $canvas.DrawImage($portrait,[Drawing.Rectangle]::new($x+4,$y+24,312,234)) } finally { $portrait.Dispose() }
            $canvas.DrawString(('{0:00}' -f $record.CharacterId),$font,[Drawing.Brushes]::White,$x+6,$y)
        }
        $sheet.Save((Join-Path $PSScriptRoot ('overview-{0:00}.png' -f ($page+1))),[Drawing.Imaging.ImageFormat]::Png)
    } finally { $font.Dispose(); $canvas.Dispose(); $sheet.Dispose() }
}
"PASS: 60 selected 4:3 originals, 60 game tiles, manifest and gallery packaged."
