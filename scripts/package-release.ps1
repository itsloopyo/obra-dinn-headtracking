#!/usr/bin/env pwsh
#Requires -Version 5.1
# Thin wrapper: calls shared packaging script with Obra Dinn values.

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir

& "$projectDir/cameraunlock-core/scripts/package-bepinex-mod.ps1" `
    -ModName "ObraDinnHeadTracking" `
    -CsprojPath "src/ObraDinnHeadTracking/ObraDinnHeadTracking.csproj" `
    -BuildOutputDir "src/ObraDinnHeadTracking/bin/Release/net35" `
    -ModDlls @("ObraDinnHeadTracking.dll","CameraUnlock.Core.dll","CameraUnlock.Core.Unity.dll") `
    -ProjectRoot $projectDir
