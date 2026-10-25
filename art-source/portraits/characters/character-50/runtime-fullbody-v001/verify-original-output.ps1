$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../../../../../')).Path
$rows=Get-Content -Raw (Join-Path $PSScriptRoot 'backup-manifest.json')|ConvertFrom-Json
$normal=0;$news=0
foreach($r in $rows){
 $live=Join-Path $repo ('FolderVerse/Content/Images/Portraits/'+$r.Target)
 if((Get-FileHash $live).Hash.ToLowerInvariant() -ne $r.Sha256){throw "Live changed: $($r.Target)"}
 $output=Join-Path $repo ('FolderVerse/bin/Debug/net9.0/Content/Images/Portraits/'+$r.Target)
 if($r.Target.StartsWith('newspaper/')){if((Get-FileHash $output).Hash.ToLowerInvariant() -ne $r.Sha256){throw "Output mismatch: $($r.Target)"};$news++;continue}
 $bytes=[IO.File]::ReadAllBytes([IO.Path]::ChangeExtension($output,'.xnb'));$start=$bytes.Length-256*192*4
 if($start -lt 0){throw 'Invalid XNB length'}
 $im=[Drawing.Bitmap]::FromFile($live)
 try{if($im.Width -ne 256 -or $im.Height -ne 192){throw 'Unexpected dimensions'}
 for($y=0;$y -lt 192;$y++){for($x=0;$x -lt 256;$x++){$c=$im.GetPixel($x,$y);$i=$start+($y*256+$x)*4
 if($bytes[$i] -ne [math]::Floor($c.R*$c.A/255) -or $bytes[$i+1] -ne [math]::Floor($c.G*$c.A/255) -or $bytes[$i+2] -ne [math]::Floor($c.B*$c.A/255) -or $bytes[$i+3] -ne $c.A){throw "XNB mismatch: $($r.Target) $x,$y"}
 }};$normal++}finally{$im.Dispose()}
}
if($normal -ne 12 -or $news -ne 8){throw 'Count mismatch'}
"Existing output verified: $normal original normal XNB texture buffers; $news original newspaper PNGs. No staged images adopted."
