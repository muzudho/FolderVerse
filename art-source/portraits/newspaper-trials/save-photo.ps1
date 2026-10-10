param([Parameter(Mandatory)][string]$Source,[Parameter(Mandatory)][string]$Stem)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$target=Join-Path $PSScriptRoot "$Stem.png"
$raw=Join-Path $PSScriptRoot "$Stem-generated.png"
if((Test-Path -LiteralPath $target) -or (Test-Path -LiteralPath $raw)){throw 'Output already exists'}
Copy-Item -LiteralPath $Source -Destination $raw
$inputImage=[Drawing.Image]::FromFile($raw)
try {
    if([Math]::Abs($inputImage.Width/$inputImage.Height-.75) -gt .001){throw 'Expected 3:4 source; refusing to distort/crop'}
    $bitmap=[Drawing.Bitmap]::new(1200,1600)
    try {
        $graphics=[Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $graphics.DrawImage($inputImage,[Drawing.Rectangle]::new(0,0,1200,1600))
        } finally {$graphics.Dispose()}
        $bitmap.Save($target,[Drawing.Imaging.ImageFormat]::Png)
    } finally {$bitmap.Dispose()}
} finally {$inputImage.Dispose()}
$verify=[Drawing.Image]::FromFile($target)
try {if($verify.Width -ne 1200 -or $verify.Height -ne 1600){throw 'Size verification failed'}}finally{$verify.Dispose()}
Write-Output "$Stem : 1200 x 1600 / $((Get-FileHash -LiteralPath $target).Hash)"
