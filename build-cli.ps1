<#
.SYNOPSIS
    Build self-contained executables for multiple platforms (no .NET runtime required).
.DESCRIPTION
    Publishes MqManager.Cli as a self-contained, single-file executable for each specified
    runtime identifier. Output goes to ./publish/<rid>/.
.PARAMETER Runtimes
    Comma-separated list of RIDs to build. Defaults to all supported platforms.
.PARAMETER Configuration
    Build configuration. Defaults to "Release".
.PARAMETER SingleFile
    Bundle into a single executable. Defaults to $true.
.PARAMETER Trimmed
    Enable IL trimming to reduce size. Defaults to $false (may break reflection-heavy libs).
.EXAMPLE
    .\build.ps1
    .\build.ps1 -Runtimes "win-x64,linux-x64"
    .\build.ps1 -Configuration Debug -Trimmed $false
#>

param(
    [string]$Runtimes = "win-x64,win-arm64,linux-x64,linux-arm64,osx-x64,osx-arm64",
    [string]$Configuration = "Release",
    [bool]$SingleFile = $true,
    [bool]$Trimmed = $true
)

$ErrorActionPreference = "Stop"

$dotnet = "dotnet"
$project = "src/MqManager.Cli/MqManager.Cli.csproj"
$outputRoot = "publish"

$rids = $Runtimes -split "," | ForEach-Object { $_.Trim() }

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " MqManager.Cli Multi-Platform Build" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration : $Configuration"
Write-Host "SingleFile    : $SingleFile"
Write-Host "Trimmed       : $Trimmed"
Write-Host "Runtimes      : $($rids -join ', ')"
Write-Host ""

$success = @()
$failed = @()

foreach ($rid in $rids) {
    $outputDir = Join-Path $outputRoot $rid
    Write-Host "----------------------------------------" -ForegroundColor Yellow
    Write-Host " Building: $rid" -ForegroundColor Yellow
    Write-Host "----------------------------------------" -ForegroundColor Yellow

    $publishArgs = @(
        "publish", $project,
        "-c", $Configuration,
        "-r", $rid,
        "--self-contained", "true",
        "-o", $outputDir
    )

    if ($SingleFile) {
        $publishArgs += "-p:PublishSingleFile=true"
    }

    if ($Trimmed) {
        $publishArgs += "-p:PublishTrimmed=true"
    }

    & $dotnet @publishArgs

    if ($LASTEXITCODE -eq 0) {
        Write-Host " [OK] $rid -> $outputDir" -ForegroundColor Green
        $success += $rid
    } else {
        Write-Host " [FAIL] $rid" -ForegroundColor Red
        $failed += $rid
    }
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Succeeded: $($success.Count) / $($rids.Count)" -ForegroundColor Green

if ($failed.Count -gt 0) {
    Write-Host " Failed   : $($failed -join ', ')" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Output directory: ./$outputRoot/" -ForegroundColor Cyan
Write-Host ""

# Show output sizes
foreach ($rid in $success) {
    $dir = Join-Path $outputRoot $rid
    $exe = if ($rid.StartsWith("win")) { "mqmanager.exe" } else { "mqmanager" }
    $exePath = Join-Path $dir $exe
    if (Test-Path $exePath) {
        $size = (Get-Item $exePath).Length / 1MB
        Write-Host ("  {0,-16} {1:N1} MB" -f $rid, $size)
    }
}
