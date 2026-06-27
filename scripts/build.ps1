[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release", "Test", "Verify", "Portable", "SelfContained", "Installer", "FullRelease")]
    [string]$Mode = "Debug",

    [string]$Runtime = "",

    [switch]$Clean
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "build.common.ps1")

$BuildConfig = Get-NeoTwitchBuildConfig
$Runtime = if ([string]::IsNullOrWhiteSpace($Runtime)) { $BuildConfig.defaultRuntime } else { $Runtime }
$Solution = Resolve-NeoTwitchPath $BuildConfig.solution
$AppProject = Resolve-NeoTwitchPath $BuildConfig.appProject
$InstallerProject = Resolve-NeoTwitchPath $BuildConfig.installerProject
$TestProject = Resolve-NeoTwitchPath $BuildConfig.testProject
$DebugConfiguration = $BuildConfig.debugConfiguration
$ReleaseConfiguration = $BuildConfig.releaseConfiguration

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

    $output = Join-Path $ArtifactRoot (Expand-NeoTwitchPattern $BuildConfig.portableDirectoryPattern $Version)
    $zip = Join-Path $ArtifactRoot (Expand-NeoTwitchPattern $BuildConfig.portableZipPattern $Version)

    Invoke-DotNet @(
        "publish", $AppProject,
        "-c", $ReleaseConfiguration,
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

    $output = Join-Path $ArtifactRoot $BuildConfig.selfContainedWorkDirectory
    Invoke-DotNet @(
        "publish", $AppProject,
        "-c", $ReleaseConfiguration,
        "-r", $Runtime,
        "--self-contained", "true",
        "-p:PublishSingleFile=true",
        "-p:IncludeNativeLibrariesForSelfExtract=true",
        "-p:EnableCompressionInSingleFile=true",
        "-o", $output
    )

    Copy-Item -LiteralPath (Join-Path $output $BuildConfig.appExecutable) -Destination (Join-Path $ArtifactRoot $BuildConfig.appExecutable) -Force
}

function Publish-Installer {
    param([string]$ArtifactRoot)

    $output = Join-Path $ArtifactRoot $BuildConfig.installerWorkDirectory
    Invoke-DotNet @(
        "publish", $InstallerProject,
        "-c", $ReleaseConfiguration,
        "-r", $Runtime,
        "--self-contained", "true",
        "-p:PublishSingleFile=true",
        "-p:IncludeNativeLibrariesForSelfExtract=true",
        "-p:EnableCompressionInSingleFile=true",
        "-o", $output
    )

    Copy-Item -LiteralPath (Join-Path $output $BuildConfig.installerExecutable) -Destination (Join-Path $ArtifactRoot $BuildConfig.installerExecutable) -Force
}

$Version = Get-NeoTwitchVersion $BuildConfig
$ArtifactRoot = Get-NeoTwitchArtifactRoot $BuildConfig $Version

if ($Clean -and (Test-Path -LiteralPath $ArtifactRoot)) {
    Remove-Item -LiteralPath $ArtifactRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $ArtifactRoot -Force | Out-Null

switch ($Mode) {
    "Debug" {
        Invoke-DotNet @("build", $AppProject, "-c", $DebugConfiguration)
    }
    "Release" {
        Invoke-DotNet @("build", $AppProject, "-c", $ReleaseConfiguration)
        Invoke-DotNet @("build", $InstallerProject, "-c", $ReleaseConfiguration)
    }
    "Test" {
        Invoke-DotNet @("run", "--project", $TestProject, "-c", $DebugConfiguration)
    }
    "Verify" {
        Invoke-DotNet @("run", "--project", $TestProject, "-c", $DebugConfiguration)
        Invoke-DotNet @("build", $Solution, "-c", $DebugConfiguration)
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
        Invoke-DotNet @("run", "--project", $TestProject, "-c", $ReleaseConfiguration)
        Publish-Portable $ArtifactRoot
        Publish-SelfContained $ArtifactRoot
        Publish-Installer $ArtifactRoot
    }
}

Write-Host "Build listo: $Mode -> $ArtifactRoot" -ForegroundColor Green
