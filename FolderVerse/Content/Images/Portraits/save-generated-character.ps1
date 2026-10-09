[CmdletBinding()]
param([Parameter(Mandatory)][int]$CharacterId,[Parameter(Mandatory)][int]$Variant,[Parameter(Mandatory)][string]$GeneratedSource)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$archive=Join-Path $PSScriptRoot '../../../../art-source/portraits/characters'
$records=Get-Content (Join-Path $archive 'catalog.json') -Raw | ConvertFrom-Json
$next=1+($records | Where-Object {$_.CharacterId -eq $CharacterId -and $_.Variant -eq $Variant} | Measure-Object Version -Maximum).Maximum
$name='character-{0:00}-{1}-v{2:000}' -f $CharacterId,$Variant,$next
$dir=Join-Path $archive ('character-{0:00}' -f $CharacterId)
$raw=Join-Path $dir ($name+'-generated.png')
[IO.File]::Copy((Resolve-Path -LiteralPath $GeneratedSource).Path,$raw,$false)
$im=[Drawing.Bitmap]::FromFile($raw)
try {
 if($im.Width*3 -ne $im.Height*4){throw "Generated image must have exact 4:3 aspect ratio, got $($im.Width)x$($im.Height). Raw output preserved: $raw"}
 $small=[Drawing.Bitmap]::new(256,192)
 try {
  $g=[Drawing.Graphics]::FromImage($small)
  try {$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality;$g.DrawImage($im,[Drawing.Rectangle]::new(0,0,256,192))} finally {$g.Dispose()}
  $temp=Join-Path $dir ($name+'-pending.png')
  $small.Save($temp,[Drawing.Imaging.ImageFormat]::Png)
  & (Join-Path $PSScriptRoot 'add-character-version.ps1') -CharacterId $CharacterId -Variant $Variant -Source $temp -GeneratedOriginal $raw -Note ('4:3描き足し。組み込みimage_genの出力を縦横比維持で256x192へ縮小。生成原本：'+$name+'-generated.png')
  # Only remove our staging copy after registration succeeded.
  [IO.File]::Delete($temp)
 } finally {$small.Dispose()}
} finally {$im.Dispose()}