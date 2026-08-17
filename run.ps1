$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$dotnet = Join-Path $projectRoot '.dotnet\dotnet.exe'
$project = '.\LeafReader\LeafReader.csproj'
$executable = Join-Path $projectRoot 'LeafReader\bin\Debug\net8.0-windows\LeafReader.exe'

Set-Location -LiteralPath $projectRoot

if (-not (Test-Path -LiteralPath $dotnet)) {
    throw 'Project-local .NET SDK was not found. Install .NET 8 SDK first.'
}

& $dotnet build $project -c Debug --no-restore
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$process = Start-Process -FilePath $executable -PassThru
Start-Sleep -Milliseconds 1200
if ($process.HasExited) {
    throw 'LeafReader closed during startup. Keep this window open and report the error shown above.'
}
