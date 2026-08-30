[CmdletBinding()]
param(
    [string]$Version = "",
    [switch]$Clean,

    [Parameter(Mandatory = $true)]
    [string]$SigningKeyPath
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "build.common.ps1")

$BuildConfig = Get-NeoTwitchBuildConfig
$publicKeyPath = Resolve-NeoTwitchPath "NeoTwitch.Installer/ReleaseIntegrityPublicKey.pem"
if (-not (Test-Path -LiteralPath $publicKeyPath)) {
    throw "Falta $publicKeyPath. Configura y versiona solamente la clave publica antes de preparar un release."
}

& (Join-Path $PSScriptRoot "validate-release-trust.ps1")

if (-not (Test-Path -LiteralPath $SigningKeyPath)) {
    throw "No existe la clave privada indicada: $SigningKeyPath"
}

if (-not [string]::IsNullOrWhiteSpace($Version)) {
    $normalizedVersion = Set-NeoTwitchVersion $BuildConfig $Version
    Write-Host "Version actualizada a $normalizedVersion en Directory.Build.props." -ForegroundColor Green
}

& (Join-Path $PSScriptRoot "build.ps1") -Mode FullRelease -Clean:$Clean

$releaseVersion = Get-NeoTwitchVersion $BuildConfig
$artifactRoot = Get-NeoTwitchArtifactRoot $BuildConfig $releaseVersion
& (Join-Path $PSScriptRoot "sign-release.ps1") `
    -ArtifactDirectory $artifactRoot `
    -Version $releaseVersion `
    -PrivateKeyPath $SigningKeyPath
