<#
.SYNOPSIS
    Build self-contained Desktop executables for multiple platforms.
.DESCRIPTION
    Publishes MqManager.Desktop as a self-contained, single-file, trimmed executable
    for each specified runtime identifier. Output goes to ./publish-desktop/<rid>/.
.PARAMETER Runtimes
    Comma-separated list of RIDs to build. Defaults to win-x64,linux-x64,osx-x64.
.PARAMETER Configuration
    Build configuration. Defaults to "Release".
.EXAMPLE
    .\build-desktop.ps1
    .\build-desktop.ps1 -Runtimes "win-x64,osx-arm64"
    .\build-desktop.ps1 -Configuration Debug
#>

param(
    [string]$Runtimes = "win-x64,linux-x64,osx-x64",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$dotnet = "dotnet"
$project = "src/MqManager.Desktop/MqManager.Desktop.csproj"
$outputRoot = "publish-desktop"

$rids = $Runtimes -split "," | ForEach-Object { $_.Trim() }

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " MqManager.Desktop Multi-Platform Build" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration : $Configuration"
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
        "-p:PublishSingleFile=true",
        "-p:IncludeNativeLibrariesForSelfExtract=true",
        "-p:EnableCompressionInSingleFile=true",
        "-p:PublishTrimmed=true",
        "-p:TrimMode=partial",
        "-p:DebugType=none",
        "-o", $outputDir
    )

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
    $exe = if ($rid.StartsWith("win")) { "MqManagerDesktop.exe" } else { "MqManagerDesktop" }
    $exePath = Join-Path $dir $exe
    if (Test-Path $exePath) {
        $size = (Get-Item $exePath).Length / 1MB
        Write-Host ("  {0,-16} {1:N1} MB" -f $rid, $size)
    }
}
