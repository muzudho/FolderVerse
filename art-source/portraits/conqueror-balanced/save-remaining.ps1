param([int]$CharacterId,[int]$Variant,[string]$Source,[string]$Note)
$ErrorActionPreference='Stop'
$stem='character-{0:D2}-{1}-balanced' -f $CharacterId,$Variant
$target=Join-Path $PSScriptRoot "remaining-variants/$stem.png"
if(Test-Path -LiteralPath $target){throw "Already saved: $target"}
Copy-Item -LiteralPath $Source -Destination $target
$file=Join-Path $PSScriptRoot 'remaining-progress.json'
$m=Get-Content -LiteralPath $file -Raw | ConvertFrom-Json
$m.Portraits+=[pscustomobject]@{CharacterId=$CharacterId;Variant=$Variant;File="remaining-variants/$stem.png";Sha256=(Get-FileHash -LiteralPath $target).Hash;Reviewed=$true;Note=$Note}
$m.Completed=$m.Portraits.Count
[IO.File]::WriteAllText($file,($m|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
Write-Output "Saved $stem; $($m.Completed)/$($m.Total)"
