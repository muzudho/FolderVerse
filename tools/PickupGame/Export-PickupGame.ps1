param(
    [Parameter(Mandatory)][string]$HarnessDirectory,
    [Parameter(Mandatory)][string]$InputSave,
    [Parameter(Mandatory)][string]$GameDll,
    [Parameter(Mandatory)][string]$RuntimeDirectory,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [Parameter(Mandatory)][string]$ArtifactsDirectory
)
$ErrorActionPreference='Stop'
if(Test-Path $OutputDirectory){throw 'Use a new output directory; existing reports are preserved.'}
$toolDirectory=$PSScriptRoot
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$utf8=[Text.UTF8Encoding]::new($false)
$program=[IO.File]::ReadAllText((Join-Path $HarnessDirectory 'Program.cs'))
$start='Log(new{kind="initial"'
$end='w.Campaign.Trade.Replies.Clear();if(won)break;}'
if(!$program.Contains($start) -or !$program.Contains($end)){throw 'Unsupported harness: observation points not found.'}
$program=$program.Replace($start,'SnapshotExport.Capture(w,args[2],true);'+$start)
$program=$program.Replace($end,'SnapshotExport.Capture(w,args[2],won);'+$end)
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'Program.cs'),$program,$utf8)
Copy-Item -LiteralPath (Join-Path $toolDirectory 'SnapshotExport.cs') -Destination $OutputDirectory
$project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="Program.cs"/><Compile Include="SnapshotExport.cs"/><Reference Include="FolderVerse" HintPath="$(GameDll)"/><Reference Include="MonoGame.Framework" HintPath="$(RuntimeDir)/MonoGame.Framework.dll"/></ItemGroup></Project>'
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'Pickup.csproj'),$project,$utf8)
$metadata=@{createdJst=[TimeZoneInfo]::ConvertTimeBySystemTimeZoneId([DateTime]::UtcNow,'Tokyo Standard Time').ToString('yyyy-MM-dd HH:mm:ss');input=$InputSave;inputSha256=(Get-FileHash -LiteralPath $InputSave).Hash;gameDll=$GameDll;dllSha256=(Get-FileHash -LiteralPath $GameDll).Hash;harnessSha256=(Get-FileHash -LiteralPath (Join-Path $HarnessDirectory 'Program.cs')).Hash;description='固定版・診断CPU候補の再現。20全体ターンごと＋初期・決着。軍事解決→未決着なら産業処理後。ゲームのスクリーンショットではなく解析用六面図。'}
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'metadata.json'),($metadata|ConvertTo-Json),$utf8)
& dotnet build (Join-Path $OutputDirectory 'Pickup.csproj') --artifacts-path $ArtifactsDirectory "-p:GameDll=$GameDll" "-p:RuntimeDir=$RuntimeDirectory" --nologo *> (Join-Path $OutputDirectory 'build.txt')
if($LASTEXITCODE -ne 0){throw 'Build failed; inspect build.txt'}
$env:GAME_RUNTIME=$RuntimeDirectory
$exe=Join-Path $ArtifactsDirectory 'bin/Pickup/debug/Pickup.dll'
& dotnet $exe $InputSave (Join-Path $OutputDirectory 'final.fvs') $OutputDirectory > (Join-Path $OutputDirectory 'run.jsonl')
if($LASTEXITCODE -ne 0){throw 'Simulation failed; report is incomplete'}
if((Get-FileHash -LiteralPath $InputSave).Hash -ne $metadata.inputSha256){throw 'Input hash changed'}
& (Join-Path $toolDirectory 'Render-PickupGame.ps1') -Directory $OutputDirectory
