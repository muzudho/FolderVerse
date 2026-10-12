param([string[]]$Project)
$ErrorActionPreference='Stop'
$testRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../tests'))
if(!$Project){$Project=@(Get-ChildItem -LiteralPath $testRoot -Directory | Select-Object -ExpandProperty Name)}
foreach($name in $Project){
    if($name -notmatch '^[A-Za-z0-9_-]+$'){throw "Invalid test project: $name"}
    $projectPath=Join-Path $testRoot "$name/$name.csproj"
    if(!(Test-Path -LiteralPath $projectPath)){throw "Missing test project: $name"}
    try {
        & dotnet run --project $projectPath
        if($LASTEXITCODE -ne 0){throw "Test failed: $name ($LASTEXITCODE)"}
    } finally {
        # Only generated copies in this test's bin directory; originals and reports stay.
        $binRoot=[IO.Path]::GetFullPath((Join-Path $testRoot "$name/bin"))
        if(Test-Path -LiteralPath $binRoot){
            $copies=@(Get-ChildItem -LiteralPath $binRoot -Directory -Recurse | Where-Object Name -eq 'Content')
            foreach($copy in $copies){
                $target=[IO.Path]::GetFullPath($copy.FullName)
                if(!$target.StartsWith($binRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Cleanup escaped test bin directory'}
                Remove-Item -LiteralPath $target -Recurse -Force
            }
        }
    }
}
