#!/usr/bin/env pwsh
# Stage build-time references into src/ObraDinnHeadTracking/libs.
# BepInEx + Harmony come from the committed vendor zip; Unity DLLs come from
# the game's Managed folder. Runs before restore/build via pixi.

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

$unityDlls = @(
    "UnityEngine.dll",
    "UnityEngine.CoreModule.dll",
    "UnityEngine.IMGUIModule.dll",
    "UnityEngine.InputModule.dll",
    "UnityEngine.TextRenderingModule.dll",
    "UnityEngine.PhysicsModule.dll",
    "UnityEngine.UIModule.dll"
)
$missingUnity = $unityDlls | Where-Object { -not (Test-Path (Join-Path $libsPath $_)) }

if ($missingUnity.Count -gt 0) {
    $modulePath = Join-Path $projectRoot "cameraunlock-core\powershell\GamePathDetection.psm1"
    Import-Module $modulePath -Force

    $gamePath = Find-GamePath -GameId 'obra-dinn'
    if (-not $gamePath) {
        Write-Host "ERROR: Return of the Obra Dinn install not found." -ForegroundColor Red
        Write-Host "Set OBRA_DINN_PATH or install the game via Steam, then re-run." -ForegroundColor Yellow
        Write-Host "Missing Unity DLLs: $($missingUnity -join ', ')" -ForegroundColor Yellow
        exit 1
    }

    $managedPath = Join-Path $gamePath "ObraDinn_Data\Managed"
    Write-Host "Staging Unity DLLs from $managedPath..." -ForegroundColor Yellow
    foreach ($dll in $missingUnity) {
        $src = Join-Path $managedPath $dll
        if (-not (Test-Path $src)) {
            Write-Host "ERROR: $dll not present in game's Managed folder: $src" -ForegroundColor Red
            exit 1
        }
        Copy-Item $src (Join-Path $libsPath $dll) -Force
        Write-Host "  Copied: $dll" -ForegroundColor Gray
    }
}

Write-Host "libs/ ready for build." -ForegroundColor Green
