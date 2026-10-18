[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$repo=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../../../../../')).Path
$portraitRoot=Join-Path $repo 'FolderVerse/Content/Images/Portraits'
$sources=@('character-46-normal-1','character-46-normal-2','character-46-normal-3','character-46-normal-4','character-46-normal-5','character-46-normal-6')
$kinds=@('formal','election','friends','history','id','idol','paparazzi','coin')
$jobs=@()
for($i=0;$i -lt 6;$i++){$jobs += [pscustomobject]@{Id=($i+1);Source=(Join-Path $PSScriptRoot ($sources[$i]+'-generated.png'));Tile=(Join-Path $PSScriptRoot ($sources[$i]+'.png'));Width=256;Height=192;Kind='normal'}}
foreach($kind in $kinds){$jobs += [pscustomobject]@{Id=$kind;Source=(Join-Path $PSScriptRoot ('character-46-'+$kind+'-generated.png'));Tile=(Join-Path $PSScriptRoot ('character-46-'+$kind+'.png'));Width=300;Height=400;Kind='newspaper'}}
# Validate all sources before changing any runtime asset.
foreach($j in $jobs){$im=[Drawing.Bitmap]::FromFile($j.Source);try{if($im.Width*$j.Height -ne $im.Height*$j.Width){throw "Unexpected aspect ratio: $($j.Source)"}}finally{$im.Dispose()}}
$records=@()
foreach($j in $jobs){
 $im=[Drawing.Bitmap]::FromFile($j.Source)
 try{$tile=[Drawing.Bitmap]::new($j.Width,$j.Height);$g=[Drawing.Graphics]::FromImage($tile);try{$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality;$g.DrawImage($im,[Drawing.Rectangle]::new(0,0,$j.Width,$j.Height));$tile.Save($j.Tile,[Drawing.Imaging.ImageFormat]::Png)}finally{$g.Dispose();$tile.Dispose()}}finally{$im.Dispose()}
 $targets=if($j.Kind -eq 'normal'){@(('v3-standardized/tiles/character-46-'+$j.Id+'.png'),('conqueror-creation/tiles/character-46-'+$j.Id+'.png'))}else{@('newspaper/character-46-'+$j.Id+'.png')}
 foreach($relative in $targets){$target=Join-Path $portraitRoot $relative;$backup=Join-Path $PSScriptRoot ('backup/'+$relative);[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($backup))|Out-Null;if(!(Test-Path -LiteralPath $backup)){Copy-Item -LiteralPath $target -Destination $backup};Copy-Item -LiteralPath $j.Tile -Destination $target}
 $records += [ordered]@{Id=$j.Id;Kind=$j.Kind;Source=[IO.Path]::GetRelativePath($repo,$j.Source).Replace('\','/');Tile=[IO.Path]::GetRelativePath($repo,$j.Tile).Replace('\','/');Width=$j.Width;Height=$j.Height;Sha256=(Get-FileHash -LiteralPath $j.Tile).Hash;OriginalSha256=(Get-FileHash -LiteralPath $j.Source).Hash;Targets=@($targets)}
}
$manifest=[ordered]@{CharacterId=46;AdoptedAt='2026-10-19';Standard='v017-approved-20261018';Method='Built-in image_gen, visual references and written proportion rules; latest shared style; long black hair with two side buns, amber eyes, pointed elf ears, red-black-gold Chinese robe and redflower bluegem hair ornaments, scene-specific eye bandage and gloves and scene-specific props; not rendered from the 3D prototype';Backup='backup/';Images=$records}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'adoption.json'),($manifest|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
node (Join-Path $PSScriptRoot 'finish-metadata.cjs')
if($LASTEXITCODE -ne 0){throw 'Metadata update failed'}
'Adopted 6 normal portraits in both runtime sets and 8 newspaper portraits.'
