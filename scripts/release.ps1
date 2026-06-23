[CmdletBinding()]
param(
    [string]$Version = "",
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "build.common.ps1")

$BuildConfig = Get-NeoTwitchBuildConfig

if (-not [string]::IsNullOrWhiteSpace($Version)) {
    $normalizedVersion = Set-NeoTwitchVersion $BuildConfig $Version
    Write-Host "Version actualizada a $normalizedVersion en Directory.Build.props." -ForegroundColor Green
}

& (Join-Path $PSScriptRoot "build.ps1") -Mode FullRelease -Clean:$Clean
