#!/usr/bin/env pwsh
#Requires -Version 5.1
<#
.SYNOPSIS
    Build Unity stub DLLs for net35 / Unity 2017 (Obra Dinn) builds.
.DESCRIPTION
    Single source of truth for stub layout. Called by both:
      - scripts/setup-libs.ps1 (local dev: stage stubs into src/.../libs)
      - .github/workflows/build.yml (CI: stage stubs into the same libs path)

    Layout (proven working at nightly c24bdc7):
      - UnityEngine.dll: monolithic, contains every type the plugin and
        Core.Unity reference (MonoBehaviour, Camera, Input, KeyCode,
        Vector3, Mathf, GUI, Canvas, ...). Compiled from UnityStubs.cs.
      - UnityEngine.<Module>.dll: empty placeholders, exist only so
        Core.Unity's <Reference Include="UnityEngine.CoreModule"> etc.
        resolve without MSB warnings. Contain no types.

    Why monolithic, not split-per-module:
      With split modules, the C# compiler resolves `MonoBehaviour` to
      `[UnityEngine.CoreModule]MonoBehaviour` and emits that TypeRef in
      IL. At runtime under Unity 2017's real CoreModule.dll this should
      work but in practice the plugin silently fails to JIT at Awake().
      With monolithic UnityEngine.dll, the compiler resolves everything
      to `[UnityEngine]MonoBehaviour`. Real Unity 2017's UnityEngine.dll
      is full of TypeForwardedTo entries, so binding succeeds at load.

    Do NOT build UnityEngine.InputLegacyModule.dll. If it exists,
    Core.Unity.csproj's Condition="Exists(...)" reference fires and IL
    emits [UnityEngine.InputLegacyModule]Input - which doesn't exist
    on Unity 2017 and fails to JIT at runtime.

    AssemblyVersion is pinned to 0.0.0.0 to match Unity 2017's runtime
    DLLs. Without this, Mono's binder rejects the plugin at load time.

.PARAMETER LibsPath
    Directory containing UnityStubs.cs. Built DLLs are written here.
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

$mainSource = Join-Path $LibsPath 'UnityStubs.cs'
if (-not (Test-Path $mainSource)) {
    throw "UnityStubs.cs not found at: $mainSource"
}

function Build-Stub {
    param(
        [Parameter(Mandatory = $true)][string]$ModuleName,
        [Parameter(Mandatory = $true)][string]$SourceFile
    )

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
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$SourceFile" />
  </ItemGroup>
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

Write-Host "Building Unity stub assemblies into $LibsPath..." -ForegroundColor Cyan

# Monolithic UnityEngine.dll with all types.
Build-Stub -ModuleName 'UnityEngine' -SourceFile 'UnityStubs.cs'

# Empty placeholder modules, present only so Core.Unity's
# <Reference Include="UnityEngine.CoreModule"> etc. resolve. Contain no
# types - the compiler resolves all types to monolithic UnityEngine.dll.
$emptyStubPath = Join-Path $LibsPath 'EmptyStub.cs'
'// Empty stub assembly' | Out-File -FilePath $emptyStubPath -Encoding utf8
try {
    foreach ($module in @(
        'UnityEngine.CoreModule',
        'UnityEngine.IMGUIModule',
        'UnityEngine.PhysicsModule',
        'UnityEngine.UIModule',
        'UnityEngine.TextRenderingModule',
        'UnityEngine.InputModule'
    )) {
        Build-Stub -ModuleName $module -SourceFile 'EmptyStub.cs'
    }
} finally {
    Remove-Item $emptyStubPath -ErrorAction SilentlyContinue
}

Remove-Item (Join-Path $LibsPath '*.deps.json') -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $LibsPath '*.pdb')        -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $LibsPath 'obj')          -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Unity stubs ready." -ForegroundColor Green
