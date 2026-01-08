#Requires -Version 7

param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",

    # Legacy parameters kept for compatibility with older callers.
    [ValidateSet("x86", "x64")]
    [string]$Platform = "x86",
    [string]$OutputDir,
    [string]$InstallerDir,
    [switch]$Package = $false,
    [switch]$SkipBuild = $false,
    [string]$TempDir = (Join-Path $env:TEMP "PlayniteBuild"),
    [string]$LicensedDependenciesUrl,
    [switch]$SdkNuget,
    [string]$OnlineInstallerConfig
)

$ErrorActionPreference = "Stop"

if ($Package -or $SdkNuget -or $LicensedDependenciesUrl -or $OnlineInstallerConfig)
{
    Write-Warning "Packaging/SDK/installer steps were removed as part of the Avalonia-only migration."
}

if ($SkipBuild)
{
    return $true
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$solution = Join-Path $repoRoot "source\\Playnite.sln"
$tests = Join-Path $repoRoot "source\\Tests\\Playnite.Tests.Avalonia\\Playnite.Tests.Avalonia.csproj"

dotnet build $solution -m:1 -c $Configuration
dotnet test $tests -c $Configuration --no-build

return $true
