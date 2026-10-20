$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../../../../../')).Path
$a=Get-Content -Raw (Join-Path $PSScriptRoot 'adoption-partial.json')|ConvertFrom-Json
$normal=0;$news=0
foreach($r in $a.Images){
 $tile=Join-Path $repo $r.Tile
 foreach($relative in $r.Targets){
  $runtime=Join-Path $repo ('FolderVerse/Content/Images/Portraits/'+$relative)
  if((Get-FileHash $runtime).Hash -ne $r.Sha256){throw "Runtime mismatch $relative"}
  $output=Join-Path $repo ('FolderVerse/bin/Debug/net9.0/Content/Images/Portraits/'+$relative)
  if($r.Kind -eq 'newspaper'){if((Get-FileHash $output).Hash -ne $r.Sha256){throw "Output mismatch $relative"};$news++;continue}
  $output=[IO.Path]::ChangeExtension($output,'.xnb');$bytes=[IO.File]::ReadAllBytes($output);$start=$bytes.Length-256*192*4
  $im=[Drawing.Bitmap]::FromFile($tile)
  try{if($im.Width -ne 256 -or $im.Height -ne 192){throw 'Size mismatch'}
   for($y=0;$y -lt 192;$y++){for($x=0;$x -lt 256;$x++){$c=$im.GetPixel($x,$y);$i=$start+($y*256+$x)*4
    if($bytes[$i] -ne [math]::Floor($c.R*$c.A/255) -or $bytes[$i+1] -ne [math]::Floor($c.G*$c.A/255) -or $bytes[$i+2] -ne [math]::Floor($c.B*$c.A/255) -or $bytes[$i+3] -ne $c.A){throw "XNB pixel mismatch $relative $x,$y"}
   }};$normal++
  }finally{$im.Dispose()}
 }
}
if($normal -ne 12 -or $news -ne 0){throw 'Output count mismatch'}
"Matched $normal compiled normal texture buffers and $news newspaper PNG outputs."
