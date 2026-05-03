#!/usr/bin/env pwsh
# Stage build-time references into src/ObraDinnHeadTracking/libs.
# BepInEx + Harmony come from the committed vendor zip; Unity DLLs are
# built from the committed UnityStubs.*.cs source files (same stubs CI
# uses) so local IL matches CI IL exactly. Runs before restore/build
# via pixi.

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

# Unity stubs: always rebuild from source so they stay in lockstep with
# the committed UnityStubs.*.cs. Building against the user's installed
# game's real Unity DLLs would produce IL that diverges from CI's
# stub-built IL, and that divergence has bitten us in production.
& (Join-Path $scriptDir "build-unity-stubs.ps1") -LibsPath $libsPath
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Unity stub build failed" -ForegroundColor Red
    exit 1
}

Write-Host "libs/ ready for build." -ForegroundColor Green
