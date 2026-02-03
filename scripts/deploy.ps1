#!/usr/bin/env pwsh
# Deploy built mod to ObraDinn BepInEx plugins folder
# Automatically installs BepInEx 5.x if not present

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = "Stop"
$ProgressPreference = 'SilentlyContinue'

# Import shared game detection module
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$modulePath = Join-Path $projectRoot "cameraunlock-core\powershell\GamePathDetection.psm1"
Import-Module $modulePath -Force

$gameId = 'ObraDinn'
$config = Get-GameConfig -GameId $gameId

# Find game installation
$gamePath = Find-GamePath -GameId $gameId

if (-not $gamePath) {
    Write-GameNotFoundError -GameName 'Return of the Obra Dinn' -EnvVar $config.EnvVar -SteamFolder $config.SteamFolder
    exit 1
}

Write-Host "Found game at: $gamePath" -ForegroundColor Green

$managedPath = Join-Path $gamePath "ObraDinn_Data\Managed"
$libsPath = Join-Path $scriptDir "..\src\ObraDinnHeadTracking\libs"

# Ensure libs folder has required DLLs (for building)
# PhysicsModule needed for RaycastHit in UnityAimHelper, UIModule needed by CameraUnlock.Core.Unity
$requiredUnityDlls = @("UnityEngine.dll", "UnityEngine.CoreModule.dll", "UnityEngine.IMGUIModule.dll", "UnityEngine.InputModule.dll", "UnityEngine.TextRenderingModule.dll", "UnityEngine.PhysicsModule.dll", "UnityEngine.UIModule.dll")
$missingDlls = $requiredUnityDlls | Where-Object { -not (Test-Path (Join-Path $libsPath $_)) }

if ($missingDlls.Count -gt 0) {
    Write-Host "Setting up required Unity DLLs in libs folder..." -ForegroundColor Yellow

    if (-not (Test-Path $libsPath)) {
        New-Item -ItemType Directory -Path $libsPath -Force | Out-Null
    }

    foreach ($dll in $requiredUnityDlls) {
        $srcDll = Join-Path $managedPath $dll
        $destDll = Join-Path $libsPath $dll

        if (Test-Path $srcDll) {
            Copy-Item $srcDll $destDll -Force
            Write-Host "  Copied: $dll" -ForegroundColor Gray
        } else {
            Write-Host "  WARNING: $dll not found in game folder" -ForegroundColor Yellow
        }
    }
}

# Check if BepInEx is installed
$bepInExPath = Join-Path $gamePath "BepInEx"
if (-not (Test-Path $bepInExPath)) {
    Write-Host "BepInEx not found - installing automatically..." -ForegroundColor Yellow

    # Download BepInEx 5.4.23.2 (x86 for older Unity)
    $bepVersion = "5.4.23.2"
    $zipUrl = "https://github.com/BepInEx/BepInEx/releases/download/v$bepVersion/BepInEx_win_x86_$bepVersion.zip"
    $zipPath = Join-Path $env:TEMP "BepInEx.zip"

    Write-Host "  Downloading BepInEx v$bepVersion (x86)..." -ForegroundColor Gray
    Invoke-WebRequest -Uri $zipUrl -OutFile $zipPath

    Write-Host "  Extracting to game directory..." -ForegroundColor Gray
    Expand-Archive -Path $zipPath -DestinationPath $gamePath -Force

    Remove-Item $zipPath -Force

    Write-Host "BepInEx installed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "IMPORTANT: You must run the game ONCE to let BepInEx initialize." -ForegroundColor Yellow
    Write-Host "After running the game once, run this deploy script again." -ForegroundColor Yellow
    Write-Host ""
    exit 0
}

# Check if BepInEx core DLLs exist (created after first run)
$bepCorePath = Join-Path $bepInExPath "core"
if (-not (Test-Path (Join-Path $bepCorePath "BepInEx.dll"))) {
    Write-Host "BepInEx installed but not initialized." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Please run the game ONCE to let BepInEx initialize," -ForegroundColor Yellow
    Write-Host "then run this deploy script again." -ForegroundColor Yellow
    Write-Host ""
    exit 0
}

# Copy BepInEx DLLs to libs if not present
$bepLibs = @("BepInEx.dll", "0Harmony.dll")
foreach ($dll in $bepLibs) {
    $destDll = Join-Path $libsPath $dll
    if (-not (Test-Path $destDll)) {
        $srcDll = Join-Path $bepCorePath $dll
        if (Test-Path $srcDll) {
            if (-not (Test-Path $libsPath)) {
                New-Item -ItemType Directory -Path $libsPath -Force | Out-Null
            }
            Copy-Item $srcDll $destDll -Force
            Write-Host "  Copied to libs: $dll" -ForegroundColor Gray
        }
    }
}

$pluginsPath = Join-Path $bepInExPath "plugins"
if (-not (Test-Path $pluginsPath)) {
    New-Item -ItemType Directory -Path $pluginsPath -Force | Out-Null
    Write-Host "Created plugins folder: $pluginsPath" -ForegroundColor Gray
}

$buildPath = "src/ObraDinnHeadTracking/bin/$Configuration/net35"

# Validate build output exists
if (-not (Test-Path $buildPath)) {
    Write-Host "ERROR: Build output not found at $buildPath" -ForegroundColor Red
    Write-Host "Please run 'pixi run build' first" -ForegroundColor Yellow
    exit 1
}

Write-Host "Deploying ObraDinnHeadTracking ($Configuration) to BepInEx..." -ForegroundColor Green
Write-Host "  Source: $buildPath" -ForegroundColor Gray
Write-Host "  Target: $pluginsPath" -ForegroundColor Gray

# Clean up old/legacy files that may conflict
$legacyFiles = @("HeadTracking.Core.dll", "ObraDinn.HeadTracking.dll")
foreach ($legacyFile in $legacyFiles) {
    $legacyPath = Join-Path $pluginsPath $legacyFile
    if (Test-Path $legacyPath) {
        Remove-Item $legacyPath -Force
        Write-Host "  Removed legacy file: $legacyFile" -ForegroundColor Yellow
    }
}

# Copy DLLs
Copy-Item "$buildPath/ObraDinnHeadTracking.dll" $pluginsPath -Force
Copy-Item "$buildPath/CameraUnlock.Core.dll" $pluginsPath -Force
Copy-Item "$buildPath/CameraUnlock.Core.Unity.dll" $pluginsPath -Force
Write-Host "  Copied: ObraDinnHeadTracking.dll, CameraUnlock.Core.dll, CameraUnlock.Core.Unity.dll" -ForegroundColor Gray

# Copy PDB if exists
if (Test-Path "$buildPath/ObraDinnHeadTracking.pdb") {
    Copy-Item "$buildPath/ObraDinnHeadTracking.pdb" $pluginsPath -Force
    Write-Host "  Copied: ObraDinnHeadTracking.pdb" -ForegroundColor Gray
}

Write-Host '' -ForegroundColor Green
Write-Host "[OK] Deployment complete!" -ForegroundColor Green
Write-Host "DLL location: $pluginsPath/ObraDinnHeadTracking.dll" -ForegroundColor Cyan
Write-Host '' -ForegroundColor Green
Write-Host "Launch ObraDinn to test your changes." -ForegroundColor Yellow
