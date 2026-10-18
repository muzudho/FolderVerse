$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$kinds=@('formal','election','friends','history','id','idol','paparazzi','coin')
$sheet=[Drawing.Bitmap]::new(1200,800)
$sg=[Drawing.Graphics]::FromImage($sheet)
try{
 for($index=0;$index -lt $kinds.Count;$index++){
  $kind=$kinds[$index]
  $im=[Drawing.Bitmap]::FromFile((Join-Path $PSScriptRoot "character-43-$kind-generated.png"))
  try{
   if($im.Width*4 -ne $im.Height*3){throw "Unexpected ratio: $kind $($im.Width)x$($im.Height)"}
   $tile=[Drawing.Bitmap]::new(300,400);$g=[Drawing.Graphics]::FromImage($tile)
   try{
    $g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.DrawImage($im,[Drawing.Rectangle]::new(0,0,300,400))
    $tile.Save((Join-Path $PSScriptRoot "character-43-$kind.png"),[Drawing.Imaging.ImageFormat]::Png)
    $gray=[Drawing.Bitmap]::new(300,400)
    try{for($y=0;$y -lt 400;$y++){for($x=0;$x -lt 300;$x++){$c=$tile.GetPixel($x,$y);$v=[int](0.299*$c.R+0.587*$c.G+0.114*$c.B);$gray.SetPixel($x,$y,[Drawing.Color]::FromArgb($v,$v,$v))}}
     $sg.DrawImage($gray,[int](($index%4)*300),[int]([math]::Floor($index/4)*400))
    }finally{$gray.Dispose()}
   }finally{$g.Dispose();$tile.Dispose()}
  }finally{$im.Dispose()}
 }
 $sheet.Save((Join-Path $PSScriptRoot 'newspaper-grayscale-review.png'),[Drawing.Imaging.ImageFormat]::Png)
}finally{$sg.Dispose();$sheet.Dispose()}
