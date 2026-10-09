param([int]$FirstCharacter=1,[int]$LastCharacter=60)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$archive=Join-Path $PSScriptRoot '../../../../art-source/portraits/characters'
$records=@(Get-Content (Join-Path $archive 'catalog.json') -Raw | ConvertFrom-Json | Where-Object Current)
$font=[Drawing.Font]::new('Arial',12)
try {for($first=$FirstCharacter;$first -le $LastCharacter;$first+=4){
 $last=[Math]::Min($first+3,$LastCharacter)
 $atlas=[Drawing.Bitmap]::new(1536,($last-$first+1)*212)
 $g=[Drawing.Graphics]::FromImage($atlas);$g.Clear([Drawing.Color]::Black)
 try {foreach($r in ($records | Where-Object {$_.CharacterId -ge $first -and $_.CharacterId -le $last})){
  $x=($r.Variant-1)*256;$y=($r.CharacterId-$first)*212
  $g.DrawString(('#{0:00}-{1} v{2:000}' -f $r.CharacterId,$r.Variant,$r.Version),$font,[Drawing.Brushes]::White,$x,$y)
  $im=[Drawing.Bitmap]::FromFile((Join-Path $archive $r.File))
  try {$g.DrawImage($im,[Drawing.Rectangle]::new($x,$y+20,256,192))}finally{$im.Dispose()}
 }}finally{$g.Dispose()}
 $path=Join-Path $archive ('standardization-preview-{0:00}-{1:00}.png' -f $first,$last)
 try {$atlas.Save($path,[Drawing.Imaging.ImageFormat]::Png)}finally{$atlas.Dispose()}
 Write-Output $path
}}finally{$font.Dispose()}