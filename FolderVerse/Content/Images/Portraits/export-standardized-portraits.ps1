[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$archive=Join-Path $PSScriptRoot '../../../../art-source/portraits/characters'
$records=@(Get-Content (Join-Path $archive 'catalog.json') -Raw | ConvertFrom-Json | Where-Object Current)
if($records.Count -ne 360){throw 'Exactly 360 current images are required.'}
foreach($id in 1..60){foreach($v in 1..6){$match=@($records | Where-Object {$_.CharacterId -eq $id -and $_.Variant -eq $v});if($match.Count -ne 1){throw "Missing or duplicate portrait: $id-$v"}}}
foreach($r in $records){
 $path=Join-Path $archive $r.File
 $im=[Drawing.Bitmap]::FromFile($path)
 try {if($im.Width -ne 256 -or $im.Height -ne 192){throw "Not standardized: $($r.File) ($($im.Width)x$($im.Height))"}}finally{$im.Dispose()}
 if((Get-FileHash -LiteralPath $path).Hash -ne $r.Sha256){throw "Hash mismatch: $($r.File)"}
 $raw=Join-Path $archive ($r.File -replace '\.png$','-generated.png')
 if(!(Test-Path -LiteralPath $raw)){throw "Generated original missing: $raw"}
}
$release=Join-Path $PSScriptRoot 'v3-standardized'
[IO.Directory]::CreateDirectory((Join-Path $release 'tiles')) | Out-Null
foreach($r in $records){$target=Join-Path $release ('tiles/character-{0:00}-{1}.png' -f $r.CharacterId,$r.Variant);[IO.File]::Copy((Join-Path $archive $r.File),$target,$true)}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'v2-toy-style/manifest.json') -Destination (Join-Path $release 'manifest.json')
$records | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $release 'selected-versions.json') -Encoding utf8
$contentFile=Join-Path $PSScriptRoot '../../Content.mgcb'
$content=[IO.File]::ReadAllText($contentFile)
foreach($id in 1..60){foreach($v in 1..6){
 $asset='Images/Portraits/v3-standardized/tiles/character-{0:00}-{1}.png' -f $id,$v
 if(!$content.Contains('/build:'+$asset)){
 $content += "`r`n#begin $asset`r`n/importer:TextureImporter`r`n/processor:TextureProcessor`r`n/processorParam:ColorKeyEnabled=False`r`n/processorParam:GenerateMipmaps=False`r`n/processorParam:PremultiplyAlpha=True`r`n/processorParam:ResizeToPowerOfTwo=False`r`n/processorParam:MakeSquare=False`r`n/processorParam:TextureFormat=Color`r`n/build:$asset`r`n"
 }
}}
[IO.File]::WriteAllText($contentFile,$content,[Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'active-version.txt'),"v3-standardized`r`n",[Text.UTF8Encoding]::new($false))
'Exported all 360 standardized portraits and selected v3-standardized.'