#!/usr/bin/env pwsh
#Requires -Version 5.1
# Custom packaging for Obra Dinn Head Tracking.
# Produces two ZIPs:
#   - ObraDinnHeadTracking-v{version}-installer.zip    (GitHub Release: install.cmd + plugins/ + docs)
#   - ObraDinnHeadTracking-v{version}-nexus.zip         (Nexus Mods: extract-to-game-folder layout)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ProgressPreference = 'SilentlyContinue'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir

Import-Module (Join-Path $projectDir "cameraunlock-core\powershell\ReleaseWorkflow.psm1") -Force

$csprojPath = Join-Path $projectDir "src\ObraDinnHeadTracking\ObraDinnHeadTracking.csproj"
$version = Get-CsprojVersion $csprojPath

$buildOutputDir = Join-Path $projectDir "src\ObraDinnHeadTracking\bin\Release\net35"
$scriptsDir = Join-Path $projectDir "scripts"
$releaseDir = Join-Path $projectDir "release"

$modDlls = @("ObraDinnHeadTracking.dll", "CameraUnlock.Core.dll", "CameraUnlock.Core.Unity.dll")

Write-Host "=== Obra Dinn Head Tracking - Package Release ===" -ForegroundColor Magenta
Write-Host ""
Write-Host "Version: $version" -ForegroundColor Cyan
Write-Host ""

# Validate all DLLs exist
foreach ($dll in $modDlls) {
    $dllPath = Join-Path $buildOutputDir $dll
    if (-not (Test-Path $dllPath)) {
        throw "Required DLL not found: $dllPath"
    }
}

# Validate required scripts
foreach ($script in @("install.cmd", "uninstall.cmd")) {
    $scriptPath = Join-Path $scriptsDir $script
    if (-not (Test-Path $scriptPath)) {
        throw "Required script not found: $scriptPath"
    }
}

# Create release directory
if (-not (Test-Path $releaseDir)) {
    New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
}

# --- GitHub Release ZIP (with installer) ---

Write-Host "--- GitHub Release ZIP ---" -ForegroundColor Yellow
Write-Host ""

$ghStagingDir = Join-Path $releaseDir "staging-github"
if (Test-Path $ghStagingDir) { Remove-Item -Recurse -Force $ghStagingDir }
New-Item -ItemType Directory -Path $ghStagingDir -Force | Out-Null

# Copy install/uninstall scripts
foreach ($script in @("install.cmd", "uninstall.cmd")) {
    Copy-Item (Join-Path $scriptsDir $script) -Destination $ghStagingDir -Force
    Write-Host "  $script" -ForegroundColor Green
}

# Canonical launcher manifest. Stamp the real release version into
# mod_info.version and drop it at the installer ZIP root. The launcher reads
# this file (launcher-manifest.json - never mod.json) to deploy the package
# natively from files/loader/runtime_requirements/dependencies.
$manifestSource = Join-Path $projectDir "launcher-manifest.json"
if (-not (Test-Path $manifestSource)) {
    throw "launcher-manifest.json not found at repo root: $manifestSource"
}
$manifestJson = Get-Content $manifestSource -Raw | ConvertFrom-Json
$manifestJson.mod_info.version = $version
# Set-Content -Encoding UTF8 on Windows PowerShell 5.1 writes a BOM, which
# serde_json rejects. Write through the .NET API with a no-BOM encoder.
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText(
    (Join-Path $ghStagingDir "launcher-manifest.json"),
    ($manifestJson | ConvertTo-Json -Depth 10),
    $utf8NoBom
)
Write-Host "  launcher-manifest.json (v$version)" -ForegroundColor Green

# Copy mod DLLs to plugins subfolder
$pluginsDir = Join-Path $ghStagingDir "plugins"
New-Item -ItemType Directory -Path $pluginsDir -Force | Out-Null

foreach ($dll in $modDlls) {
    Copy-Item (Join-Path $buildOutputDir $dll) -Destination $pluginsDir -Force
    Write-Host "  plugins/$dll" -ForegroundColor Green
}

# Copy vendored loader (install.cmd extracts this at runtime)
$vendorSrc = Join-Path $projectDir "vendor\bepinex"
if (-not (Test-Path $vendorSrc)) {
    throw "Vendored BepInEx folder not found: $vendorSrc. Run 'pixi run update-deps' to populate it, then commit."
}
$vendorZipSrc = Join-Path $vendorSrc "BepInEx_win_x86.zip"
if (-not (Test-Path $vendorZipSrc)) {
    throw "Vendored BepInEx zip not found: $vendorZipSrc. Run 'pixi run update-deps' to refresh, then commit."
}

$vendorDest = Join-Path $ghStagingDir "vendor\bepinex"
New-Item -ItemType Directory -Path $vendorDest -Force | Out-Null

# Per AGENTS.md: vendor dirs ship the loader zip + LICENSE + README.md only - no scripts.
foreach ($vendorFile in @("BepInEx_win_x86.zip", "LICENSE", "README.md")) {
    $src = Join-Path $vendorSrc $vendorFile
    if (-not (Test-Path $src)) {
        throw "Required vendor file not found: $src"
    }
    Copy-Item $src -Destination $vendorDest -Force
    Write-Host "  vendor/bepinex/$vendorFile" -ForegroundColor Green
}

# Copy documentation. The installer ZIP redistributes our binaries and the
# vendored loader, so a missing licence file is a compliance failure, not a
# skippable step: throw rather than warn.
$docFiles = @("README.md", "LICENSE", "CHANGELOG.md", "THIRD-PARTY-NOTICES.md")
foreach ($doc in $docFiles) {
    $docPath = Join-Path $projectDir $doc
    if (-not (Test-Path $docPath)) {
        throw "Required document not found: $doc. Every published ZIP is a binary distribution and must carry it."
    }
    Copy-Item $docPath -Destination $ghStagingDir -Force
    Write-Host "  $doc" -ForegroundColor Green
}

# install.cmd / uninstall.cmd resolve the game via shared/find-game.ps1.
# Bundle that shim alongside them so the release ZIP is self-contained.
Copy-SharedBundle -StagingDir $ghStagingDir

$ghZipName = "ObraDinnHeadTracking-v$version-installer.zip"
$ghZipPath = Join-Path $releaseDir $ghZipName
if (Test-Path $ghZipPath) { Remove-Item $ghZipPath -Force }

Write-Host ""
Write-Host "Creating GitHub ZIP..." -ForegroundColor Cyan

Push-Location $ghStagingDir
try {
    Compress-Archive -Path ".\*" -DestinationPath $ghZipPath -Force
} finally {
    Pop-Location
}
Remove-Item -Recurse -Force $ghStagingDir

$ghZipSize = (Get-Item $ghZipPath).Length / 1KB
Write-Host ("  $ghZipPath ({0:N1} KB)" -f $ghZipSize) -ForegroundColor Green

# --- Nexus Mods ZIP (extract-to-game-folder) ---

Write-Host ""
Write-Host "--- Nexus Mods ZIP ---" -ForegroundColor Yellow
Write-Host ""

$nexusStagingDir = Join-Path $releaseDir "staging-nexus"
if (Test-Path $nexusStagingDir) { Remove-Item -Recurse -Force $nexusStagingDir }

# Mirror game directory structure: BepInEx/plugins/
# Users extract to game root, DLLs land in <game>/BepInEx/plugins/
# Does NOT include BepInEx itself (dependency)
$nexusPluginsDir = Join-Path $nexusStagingDir "BepInEx\plugins"
New-Item -ItemType Directory -Path $nexusPluginsDir -Force | Out-Null

foreach ($dll in $modDlls) {
    Copy-Item (Join-Path $buildOutputDir $dll) -Destination $nexusPluginsDir -Force
    Write-Host "  BepInEx/plugins/$dll" -ForegroundColor Green
}

$nexusZipName = "ObraDinnHeadTracking-v$version-nexus.zip"
$nexusZipPath = Join-Path $releaseDir $nexusZipName
if (Test-Path $nexusZipPath) { Remove-Item $nexusZipPath -Force }

Write-Host ""
Write-Host "Creating Nexus ZIP..." -ForegroundColor Cyan

# The Nexus ZIP is a binary distribution too: the licences of everything
# compiled into or bundled with the payload require their notices to travel
# with it, so LICENSE and THIRD-PARTY-NOTICES.md ship at its root.
foreach ($noticeDoc in @('LICENSE', 'THIRD-PARTY-NOTICES.md', 'README.md')) {
    $noticeSrc = Join-Path $projectDir $noticeDoc
    if (-not (Test-Path $noticeSrc)) {
        throw "Required notice file not found: $noticeDoc. Every published ZIP is a binary distribution and must carry it."
    }
    Copy-Item $noticeSrc -Destination $nexusStagingDir -Force
    Write-Host "  $noticeDoc" -ForegroundColor Green
}
Push-Location $nexusStagingDir
try {
    Compress-Archive -Path ".\*" -DestinationPath $nexusZipPath -Force
} finally {
    Pop-Location
}
Remove-Item -Recurse -Force $nexusStagingDir

$nexusZipSize = (Get-Item $nexusZipPath).Length / 1KB
Write-Host ("  $nexusZipPath ({0:N1} KB)" -f $nexusZipSize) -ForegroundColor Green

# --- Summary ---

Write-Host ""
Write-Host "=== Package Complete ===" -ForegroundColor Magenta
Write-Host ""
Write-Host ("GitHub Release:  $ghZipPath ({0:N1} KB)" -f $ghZipSize) -ForegroundColor Green
Write-Host ("Nexus Mods:      $nexusZipPath ({0:N1} KB)" -f $nexusZipSize) -ForegroundColor Green

# Output all zip paths for CI capture (one per line)
Write-Output $ghZipPath
Write-Output $nexusZipPath
