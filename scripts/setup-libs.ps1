#!/usr/bin/env pwsh
# Stage BepInEx + Harmony into src/ObraDinnHeadTracking/libs from the
# committed vendor zip. The Unity reference stubs are a separate step:
# `pixi run setup-libs` runs this first, then compiles them from the
# shared sources in cameraunlock-core/csharp/stubs.

$ErrorActionPreference = "Stop"
$ProgressPreference = 'SilentlyContinue'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$libsPath = Join-Path $projectRoot "src\ObraDinnHeadTracking\libs"
$vendorZip = Join-Path $projectRoot "vendor\bepinex\BepInEx_win_x86.zip"

if (-not (Test-Path $libsPath)) {
    New-Item -ItemType Directory -Path $libsPath -Force | Out-Null
}

$bepDlls = @("BepInEx.dll", "0Harmony.dll")
$missingBep = $bepDlls | Where-Object { -not (Test-Path (Join-Path $libsPath $_)) }

if ($missingBep.Count -gt 0) {
    if (-not (Test-Path $vendorZip)) {
        Write-Host "ERROR: Vendored BepInEx zip not found at: $vendorZip" -ForegroundColor Red
        Write-Host "Run 'pixi run update-deps' to refresh the vendor folder." -ForegroundColor Yellow
        exit 1
    }

    Write-Host "Staging BepInEx DLLs from vendor zip..." -ForegroundColor Yellow
    $extractDir = Join-Path $env:TEMP "obra-dinn-bep-libs-$([System.Guid]::NewGuid().ToString('N'))"
    try {
        Expand-Archive -Path $vendorZip -DestinationPath $extractDir -Force
        $coreDir = Join-Path $extractDir "BepInEx\core"
        foreach ($dll in $bepDlls) {
            $src = Join-Path $coreDir $dll
            if (-not (Test-Path $src)) {
                Write-Host "ERROR: $dll missing from vendor zip ($vendorZip)" -ForegroundColor Red
                exit 1
            }
            Copy-Item $src (Join-Path $libsPath $dll) -Force
            Write-Host "  Copied: $dll" -ForegroundColor Gray
        }
    } finally {
        if (Test-Path $extractDir) { Remove-Item $extractDir -Recurse -Force }
    }
}

Write-Host "BepInEx references staged." -ForegroundColor Green
