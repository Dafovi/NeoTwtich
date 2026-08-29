[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ArtifactDirectory,

    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$PrivateKeyPath
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "build.common.ps1")

$BuildConfig = Get-NeoTwitchBuildConfig
$artifactRoot = (Resolve-Path -LiteralPath $ArtifactDirectory).Path
$privateKey = (Resolve-Path -LiteralPath $PrivateKeyPath).Path
$publicKeyPath = Resolve-NeoTwitchPath "NeoTwitch.Installer/ReleaseIntegrityPublicKey.pem"

if (-not (Test-Path -LiteralPath $publicKeyPath)) {
    throw "Falta la clave publica de releases: $publicKeyPath"
}

$signer = [System.Security.Cryptography.ECDsa]::Create()
$verifier = [System.Security.Cryptography.ECDsa]::Create()
try {
    $signer.ImportFromPem([System.IO.File]::ReadAllText($privateKey))
    $verifier.ImportFromPem([System.IO.File]::ReadAllText($publicKeyPath))
    if ($signer.KeySize -ne 256 -or $verifier.KeySize -ne 256) {
        throw "Las claves de release deben usar ECDSA P-256."
    }

    $signerPublicKey = $signer.ExportSubjectPublicKeyInfo()
    $trustedPublicKey = $verifier.ExportSubjectPublicKeyInfo()
    if (-not [System.Security.Cryptography.CryptographicOperations]::FixedTimeEquals($signerPublicKey, $trustedPublicKey)) {
        throw "La clave privada no corresponde a ReleaseIntegrityPublicKey.pem."
    }

    $artifacts = @(
        Get-ChildItem -LiteralPath $artifactRoot -File |
            Where-Object {
                $_.Name -ne $BuildConfig.releaseIntegrityManifest -and
                $_.Name -ne $BuildConfig.releaseIntegritySignature -and
                ($_.Extension -eq ".zip" -or $_.Extension -eq ".exe")
            } |
            Sort-Object Name |
            ForEach-Object {
                [ordered]@{
                    file = $_.Name
                    sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
                    size = $_.Length
                }
            }
    )

    if ($artifacts.Count -eq 0) {
        throw "No hay artefactos .zip o .exe para firmar en $artifactRoot."
    }

    $manifest = [ordered]@{
        schemaVersion = 1
        product = $BuildConfig.productIdentifier
        version = $Version
        artifacts = $artifacts
    }

    $manifestPath = Join-Path $artifactRoot $BuildConfig.releaseIntegrityManifest
    $signaturePath = Join-Path $artifactRoot $BuildConfig.releaseIntegritySignature
    $manifestJson = $manifest | ConvertTo-Json -Depth 5
    $encoding = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($manifestPath, $manifestJson, $encoding)

    $manifestBytes = [System.IO.File]::ReadAllBytes($manifestPath)
    $signature = $signer.SignData(
        $manifestBytes,
        [System.Security.Cryptography.HashAlgorithmName]::SHA256,
        [System.Security.Cryptography.DSASignatureFormat]::IeeeP1363FixedFieldConcatenation)
    [System.IO.File]::WriteAllText($signaturePath, [Convert]::ToBase64String($signature), [System.Text.Encoding]::ASCII)

    Write-Host "Manifest firmado: $manifestPath" -ForegroundColor Green
    Write-Host "Firma: $signaturePath" -ForegroundColor Green
}
finally {
    $signer.Dispose()
    $verifier.Dispose()
}
