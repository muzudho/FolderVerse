[CmdletBinding()]
param(
 [Parameter(Mandatory)][ValidateRange(1,60)][int]$CharacterId,
 [Parameter(Mandatory)][ValidateRange(1,6)][int]$Variant,
 [Parameter(Mandatory)][string]$Source,
 [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$Note,
 [string]$ArchiveRoot=(Join-Path $PSScriptRoot 'characters')
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$ArchiveRoot=[IO.Path]::GetFullPath($ArchiveRoot)
$catalogPath=Join-Path $ArchiveRoot 'catalog.json'
$sourcePath=(Resolve-Path -LiteralPath $Source).Path
if([IO.Path]::GetExtension($sourcePath) -ine '.png'){throw 'Source must be a PNG file.'}
$image=[Drawing.Bitmap]::FromFile($sourcePath)
try{$width=$image.Width;$height=$image.Height}finally{$image.Dispose()}
# Serialize additions so two concurrent edits cannot claim the same version.
$lock=[IO.File]::Open((Join-Path $ArchiveRoot '.version-lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
try {
 $records=@(Get-Content -LiteralPath $catalogPath -Raw | ConvertFrom-Json)
 $history=@($records | Where-Object {$_.CharacterId -eq $CharacterId -and $_.Variant -eq $Variant})
 if($history.Count -eq 0){throw 'Character/variant is missing from the catalog.'}
 $version=1+($history | Measure-Object Version -Maximum).Maximum
 $name='character-{0:00}-{1}-v{2:000}.png' -f $CharacterId,$Variant,$version
 $relative='character-{0:00}/{1}' -f $CharacterId,$name
 $destination=Join-Path $ArchiveRoot $relative
 # Copy refuses to overwrite a previously saved image.
 [IO.File]::Copy($sourcePath,$destination,$false)
 foreach($record in $history){$record.Current=$false}
 $records += [pscustomobject]@{CharacterId=$CharacterId;Variant=$Variant;Version=$version;File=$relative;Width=$width;Height=$height;Sha256=(Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash;Source=$sourcePath;Note=$Note;ArchivedAt=[DateTimeOffset]::UtcNow.ToOffset([TimeSpan]::FromHours(9)).ToString('yyyy-MM-ddTHH:mm:sszzz');Current=$true}
 $pending=Join-Path $ArchiveRoot 'catalog.pending.json'
 $records | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $pending -Encoding utf8
 [IO.File]::Move($pending,$catalogPath,$true)
 Write-Output $destination
} finally {$lock.Dispose()}