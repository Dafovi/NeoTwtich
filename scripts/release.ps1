[CmdletBinding()]
param(
    [string]$Version = "",
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$VersionProps = Join-Path $RepoRoot "Directory.Build.props"

if (-not [string]::IsNullOrWhiteSpace($Version)) {
    $normalizedVersion = $Version.Trim().TrimStart("v", "V")
    [xml]$props = Get-Content -LiteralPath $VersionProps
    $props.Project.PropertyGroup.Version = $normalizedVersion
    $props.Save($VersionProps)
    Write-Host "Version actualizada a $normalizedVersion en Directory.Build.props." -ForegroundColor Green
}

& (Join-Path $PSScriptRoot "build.ps1") -Mode FullRelease -Clean:$Clean
