Add-Type -AssemblyName System.Drawing
$sourceDir = Join-Path $PSScriptRoot '..\v1-original'
$edges = @(
 @(0,172,342,512,684,838,1024),@(0,171,342,513,684,854,1024),
 @(0,170,342,513,682,836,1024),@(0,171,341,512,682,853,1024),
 @(0,170,341,513,684,837,1024),@(0,166,336,513,682,852,1024),
 @(0,158,315,473,631,791,1024),@(0,171,342,512,683,853,1024),
 @(0,170,341,511,684,853,1024),@(0,170,342,511,683,839,1024))
$records = @()
for ($s=1; $s -le 10; $s++) {
 $sheet = [System.Drawing.Bitmap]::FromFile((Join-Path $PSScriptRoot ('sheet-{0:00}.png' -f $s)))
 for ($row=0; $row -lt 6; $row++) { for ($col=0; $col -lt 6; $col++) {
  $id=($s-1)*6+$row+1
  $name='character-{0:00}-{1}.png' -f $id,($col+1)
  $old=[System.Drawing.Bitmap]::FromFile((Join-Path $sourceDir "tiles\$name"))
  $tile=New-Object System.Drawing.Bitmap($old.Width,$old.Height)
  $g=[System.Drawing.Graphics]::FromImage($tile)
  $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
  $x=[int][Math]::Round($col*$sheet.Width/6)+2
  $y=[int][Math]::Round($edges[$s-1][$row]*$sheet.Height/1024)+2
  $w=[int][Math]::Round(($col+1)*$sheet.Width/6)-$x-2
  $h=[int][Math]::Round($edges[$s-1][$row+1]*$sheet.Height/1024)-$y-2
  if($s -eq 7){$y=$row*156+2;$h=152;$g.Dispose();$tile.Dispose();$tile=New-Object System.Drawing.Bitmap(252,152);$g=[System.Drawing.Graphics]::FromImage($tile)}
  $g.DrawImage($sheet,[System.Drawing.Rectangle]::new(0,0,$tile.Width,$tile.Height),[System.Drawing.Rectangle]::new($x,$y,$w,$h),[System.Drawing.GraphicsUnit]::Pixel)
  $tile.Save((Join-Path $PSScriptRoot "tiles\$name"),[System.Drawing.Imaging.ImageFormat]::Png)
  $records+= [pscustomobject]@{File="tiles/$name";BaseId=$id-1;VariantId=$col;Sheet=('sheet-{0:00}.png' -f $s);Width=$tile.Width;Height=$tile.Height;SourceX=$x;SourceY=$y;SourceWidth=$w;SourceHeight=$h}
  $g.Dispose();$tile.Dispose();$old.Dispose()
 }}
 $sheet.Dispose()
}
$records | ConvertTo-Json -Depth 3 | Set-Content (Join-Path $PSScriptRoot 'tile-index.json') -Encoding UTF8
foreach ($id in @(6,18,30,42,60)) {
 $name='base-{0:00}-normalized.png' -f $id
 $original=[System.Drawing.Bitmap]::FromFile((Join-Path $sourceDir $name))
 $atlas=New-Object System.Drawing.Bitmap($original.Width,$original.Height)
 $g=[System.Drawing.Graphics]::FromImage($atlas)
 $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
 for($v=0;$v -lt 6;$v++){
  $tile=[System.Drawing.Bitmap]::FromFile((Join-Path $PSScriptRoot ('tiles\character-{0:00}-{1}.png' -f $id,($v+1))))
  $x=[int][Math]::Round(($v%3)*$atlas.Width/3)
  $y=[int][Math]::Round([Math]::Floor($v/3)*$atlas.Height/2)
  if($id -eq 42){$g.DrawImage($tile,[System.Drawing.Rectangle]::new($x,$y,512,512),[System.Drawing.Rectangle]::new(50,0,152,152),[System.Drawing.GraphicsUnit]::Pixel)}
  else {$g.DrawImage($tile,[System.Drawing.Rectangle]::new($x,$y,[int]($atlas.Width/3),[int]($atlas.Height/2)))}
  $tile.Dispose()
 }
 $atlas.Save((Join-Path $PSScriptRoot $name),[System.Drawing.Imaging.ImageFormat]::Png)
 $g.Dispose();$atlas.Dispose();$original.Dispose()
}
"Created $($records.Count) tiles"

