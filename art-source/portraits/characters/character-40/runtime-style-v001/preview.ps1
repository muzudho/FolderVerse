$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
for($i=1;$i -le 6;$i++){
 $source=Join-Path $PSScriptRoot "character-40-normal-$i-generated.png"
 if(!(Test-Path -LiteralPath $source)){continue}
 $im=[Drawing.Bitmap]::FromFile($source)
 try{
  if($im.Width*3 -ne $im.Height*4){throw "Unexpected aspect ratio: $source"}
  $tile=[Drawing.Bitmap]::new(256,192)
  $g=[Drawing.Graphics]::FromImage($tile)
  try{
   $g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
   $g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
   $g.DrawImage($im,[Drawing.Rectangle]::new(0,0,256,192))
   $tile.Save((Join-Path $PSScriptRoot "character-40-normal-$i.png"),[Drawing.Imaging.ImageFormat]::Png)
  }finally{$g.Dispose();$tile.Dispose()}
 }finally{$im.Dispose()}
}
# Previews only. This script never changes runtime assets or catalog selections.
