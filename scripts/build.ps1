[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release", "Portable", "SelfContained", "Installer", "FullRelease")]
    [string]$Mode = "Debug",

    [string]$Runtime = "win-x64",

    [switch]$Clean
)

$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$AppProject = Join-Path $RepoRoot "NeoTwitch\NeoTwitch.csproj"
$InstallerProject = Join-Path $RepoRoot "NeoTwitch.Installer\NeoTwitch.Installer.csproj"
$VersionProps = Join-Path $RepoRoot "Directory.Build.props"

function Get-NeoTwitchVersion {
    [xml]$props = Get-Content -LiteralPath $VersionProps
    $version = $props.Project.PropertyGroup.Version
    if ([string]::IsNullOrWhiteSpace($version)) {
        throw "No encontre <Version> en Directory.Build.props."
    }

    return $version.Trim().TrimStart("v", "V")
}

function Invoke-DotNet {
    param([string[]]$Arguments)

    Write-Host "dotnet $($Arguments -join ' ')" -ForegroundColor Cyan
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet fallo con codigo $LASTEXITCODE."
    }
}

function Publish-Portable {
    param([string]$ArtifactRoot)

    $output = Join-Path $ArtifactRoot "NeoTwitch-V$Version-Windows"
    $zip = Join-Path $ArtifactRoot "NeoTwitch-V$Version-Windows.zip"

    Invoke-DotNet @(
        "publish", $AppProject,
        "-c", "Release",
        "-r", $Runtime,
        "--self-contained", "false",
        "-p:PublishSingleFile=false",
        "-o", $output
    )

    if (Test-Path -LiteralPath $zip) {
        Remove-Item -LiteralPath $zip -Force
    }

    Compress-Archive -Path (Join-Path $output "*") -DestinationPath $zip -Force
}

function Publish-SelfContained {
    param([string]$ArtifactRoot)

    $output = Join-Path $ArtifactRoot "self-contained"
    Invoke-DotNet @(
        "publish", $AppProject,
        "-c", "Release",
        "-r", $Runtime,
        "--self-contained", "true",
        "-p:PublishSingleFile=true",
        "-p:IncludeNativeLibrariesForSelfExtract=true",
        "-p:EnableCompressionInSingleFile=true",
        "-o", $output
    )

    Copy-Item -LiteralPath (Join-Path $output "NeoTwitch.exe") -Destination (Join-Path $ArtifactRoot "NeoTwitch.exe") -Force
}

function Publish-Installer {
    param([string]$ArtifactRoot)

    $output = Join-Path $ArtifactRoot "installer"
    Invoke-DotNet @(
        "publish", $InstallerProject,
        "-c", "Release",
        "-r", $Runtime,
        "--self-contained", "true",
        "-p:PublishSingleFile=true",
        "-p:IncludeNativeLibrariesForSelfExtract=true",
        "-p:EnableCompressionInSingleFile=true",
        "-o", $output
    )

    Copy-Item -LiteralPath (Join-Path $output "NeoTwitch.Installer.exe") -Destination (Join-Path $ArtifactRoot "NeoTwitch.Installer.exe") -Force
}

$Version = Get-NeoTwitchVersion
$ArtifactRoot = Join-Path $RepoRoot "artifacts\V$Version"

if ($Clean -and (Test-Path -LiteralPath $ArtifactRoot)) {
    Remove-Item -LiteralPath $ArtifactRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $ArtifactRoot -Force | Out-Null

switch ($Mode) {
    "Debug" {
        Invoke-DotNet @("build", $AppProject, "-c", "Debug")
    }
    "Release" {
        Invoke-DotNet @("build", $AppProject, "-c", "Release")
        Invoke-DotNet @("build", $InstallerProject, "-c", "Release")
    }
    "Portable" {
        Publish-Portable $ArtifactRoot
    }
    "SelfContained" {
        Publish-SelfContained $ArtifactRoot
    }
    "Installer" {
        Publish-Installer $ArtifactRoot
    }
    "FullRelease" {
        Publish-Portable $ArtifactRoot
        Publish-SelfContained $ArtifactRoot
        Publish-Installer $ArtifactRoot
    }
}

Write-Host "Build listo: $Mode -> $ArtifactRoot" -ForegroundColor Green
