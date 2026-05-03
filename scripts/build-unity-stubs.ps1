#!/usr/bin/env pwsh
#Requires -Version 5.1
<#
.SYNOPSIS
    Build the Unity stub DLLs into a libs directory.
.DESCRIPTION
    Single source of truth for the stub layout. Called by both:
      - scripts/setup-libs.ps1 (local dev: stage stubs into src/.../libs)
      - .github/workflows/build.yml (CI: stage stubs into the same libs path)

    Local builds and CI builds must produce IL with identical assembly
    references. Without that, divergences (like Input living in
    InputLegacyModule for CI but in CoreModule for Unity 2017 at runtime)
    silently ship to users.

    The stub source files (UnityStubs.*.cs) live next to where the DLLs
    are written, in $LibsPath. The .cs files are committed; the .dll
    files are gitignored and rebuilt by this script.

    AssemblyVersion is pinned to 0.0.0.0 to match Unity 2017's runtime
    DLLs - without this, Mono fails to bind the plugin at load time.

.PARAMETER LibsPath
    Directory containing UnityStubs.*.cs source files. The built DLLs
    are written into this same directory.
#>
param(
    [Parameter(Mandatory = $true)][string]$LibsPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

if (-not (Test-Path $LibsPath)) {
    throw "LibsPath does not exist: $LibsPath"
}
$LibsPath = (Resolve-Path $LibsPath).Path

function Build-StubModule {
    param(
        [Parameter(Mandatory = $true)][string]$ModuleName,
        [Parameter(Mandatory = $true)][string]$SourceFile,
        [string[]]$References = @()
    )

    $sourcePath = Join-Path $LibsPath $SourceFile
    if (-not (Test-Path $sourcePath)) {
        throw "Stub source not found: $sourcePath"
    }

    $refXml = ""
    foreach ($r in $References) {
        # HintPath is relative to the csproj's directory. The csproj is
        # generated INTO $LibsPath, so the referenced DLL is a sibling -
        # no path prefix.
        $refXml += "    <Reference Include=`"$r`"><HintPath>$r.dll</HintPath></Reference>`n"
    }

    $projContent = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net35</TargetFramework>
    <LangVersion>7.3</LangVersion>
    <AssemblyName>$ModuleName</AssemblyName>
    <AssemblyVersion>0.0.0.0</AssemblyVersion>
    <FileVersion>0.0.0.0</FileVersion>
    <Version>0.0.0.0</Version>
    <NoWarn>CS0169;CS0649;CS0067;CS0660;CS0661</NoWarn>
    <DisableImplicitFrameworkReferences>false</DisableImplicitFrameworkReferences>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$SourceFile" />
$refXml  </ItemGroup>
</Project>
"@

    $projPath = Join-Path $LibsPath "Stub_$ModuleName.csproj"
    $projContent | Out-File -FilePath $projPath -Encoding utf8

    & dotnet build $projPath -c Release -o $LibsPath --nologo -v q 2>&1 | Out-Null
    $rc = $LASTEXITCODE
    Remove-Item $projPath -ErrorAction SilentlyContinue
    if ($rc -ne 0) {
        throw "Failed to build $ModuleName stub (exit $rc)"
    }
    Write-Host "  Built $ModuleName.dll" -ForegroundColor Gray
}

# Compile order is dependency-driven: CoreModule first (everything else
# may reference its types), then leaf modules referencing it, then UI
# which needs CoreModule + UIModule + TextRenderingModule.
#
# Layout MUST match Unity 2017's actual runtime DLL set:
#   - Input + KeyCode live in CoreModule (Unity 2018+ split them out
#     into UnityEngine.InputLegacyModule.dll, but Obra Dinn is 2017).
#   - UnityEngine.InputModule.dll is the EventSystems module - empty
#     placeholder here, present only because OBRA's csproj has a
#     hardcoded Reference to it.
#   - UnityEngine.dll is mostly type forwarders into the module DLLs.
#   - We do NOT build UnityEngine.InputLegacyModule.dll - if we did,
#     Core.Unity's Condition="Exists('...InputLegacyModule.dll')"
#     reference would fire and IL would emit
#     [UnityEngine.InputLegacyModule]Input, which fails to JIT under
#     Unity 2017 at runtime (no such DLL exists).
Write-Host "Building Unity stub assemblies into $LibsPath..." -ForegroundColor Cyan

Build-StubModule -ModuleName "UnityEngine.CoreModule"          -SourceFile "UnityStubs.CoreModule.cs"
Build-StubModule -ModuleName "UnityEngine.TextRenderingModule" -SourceFile "UnityStubs.TextRenderingModule.cs" -References @("UnityEngine.CoreModule")
Build-StubModule -ModuleName "UnityEngine.PhysicsModule"       -SourceFile "UnityStubs.PhysicsModule.cs"       -References @("UnityEngine.CoreModule")
Build-StubModule -ModuleName "UnityEngine.UIModule"            -SourceFile "UnityStubs.UIModule.cs"            -References @("UnityEngine.CoreModule")
Build-StubModule -ModuleName "UnityEngine.IMGUIModule"         -SourceFile "UnityStubs.IMGUIModule.cs"         -References @("UnityEngine.CoreModule", "UnityEngine.TextRenderingModule")
Build-StubModule -ModuleName "UnityEngine.InputModule"         -SourceFile "UnityStubs.InputModule.cs"
Build-StubModule -ModuleName "UnityEngine"                     -SourceFile "UnityStubs.Engine.cs"              -References @(
    "UnityEngine.CoreModule",
    "UnityEngine.IMGUIModule",
    "UnityEngine.PhysicsModule",
    "UnityEngine.TextRenderingModule",
    "UnityEngine.UIModule"
)
Build-StubModule -ModuleName "UnityEngine.UI"                  -SourceFile "UnityStubs.UI.cs"                  -References @("UnityEngine.CoreModule", "UnityEngine.UIModule", "UnityEngine.TextRenderingModule")

Remove-Item (Join-Path $LibsPath "*.deps.json") -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $LibsPath "*.pdb")        -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $LibsPath "obj")          -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Unity stubs ready." -ForegroundColor Green
