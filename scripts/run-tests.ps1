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
        & (Join-Path $PSScriptRoot 'clean-test-content.ps1') -Project $name
    }
}
