$Script:RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$Script:BuildConfigPath = Join-Path $Script:RepoRoot "build.config.json"

function Get-NeoTwitchBuildConfig {
    if (-not (Test-Path -LiteralPath $Script:BuildConfigPath)) {
        throw "No encontre build.config.json en $Script:RepoRoot."
    }

    return Get-Content -LiteralPath $Script:BuildConfigPath -Raw | ConvertFrom-Json
}

function Resolve-NeoTwitchPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path $Script:RepoRoot $Path
}

function Expand-NeoTwitchPattern {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Pattern,

        [Parameter(Mandatory = $true)]
        [string]$Version
    )

    return $Pattern.Replace("{version}", $Version)
}

function Get-NeoTwitchVersion {
    param(
        [Parameter(Mandatory = $true)]
        $BuildConfig
    )

    $versionProps = Resolve-NeoTwitchPath $BuildConfig.versionProps
    [xml]$props = Get-Content -LiteralPath $versionProps
    $version = $props.Project.PropertyGroup.Version
    if ([string]::IsNullOrWhiteSpace($version)) {
        throw "No encontre <Version> en $versionProps."
    }

    return $version.Trim().TrimStart("v", "V")
}

function Set-NeoTwitchVersion {
    param(
        [Parameter(Mandatory = $true)]
        $BuildConfig,

        [Parameter(Mandatory = $true)]
        [string]$Version
    )

    $normalizedVersion = $Version.Trim().TrimStart("v", "V")
    $versionProps = Resolve-NeoTwitchPath $BuildConfig.versionProps
    [xml]$props = Get-Content -LiteralPath $versionProps
    $props.Project.PropertyGroup.Version = $normalizedVersion
    $props.Save($versionProps)

    return $normalizedVersion
}

function Get-NeoTwitchArtifactRoot {
    param(
        [Parameter(Mandatory = $true)]
        $BuildConfig,

        [Parameter(Mandatory = $true)]
        [string]$Version
    )

    return Join-Path (Resolve-NeoTwitchPath $BuildConfig.artifactsDirectory) "V$Version"
}
