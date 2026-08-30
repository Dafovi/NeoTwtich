[CmdletBinding()]
param(
    [string]$InstallerAssemblyPath = ""
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "build.common.ps1")

$publicKeyPath = Resolve-NeoTwitchPath "NeoTwitch.Installer/ReleaseIntegrityPublicKey.pem"
if (-not (Test-Path -LiteralPath $publicKeyPath)) {
    throw "Falta la clave publica de produccion: $publicKeyPath"
}

$key = [System.Security.Cryptography.ECDsa]::Create()
try {
    $key.ImportFromPem([System.IO.File]::ReadAllText($publicKeyPath))
    if ($key.KeySize -ne 256) { throw "La clave publica de produccion debe usar ECDSA P-256." }
}
finally {
    $key.Dispose()
}

if (-not [string]::IsNullOrWhiteSpace($InstallerAssemblyPath)) {
    $assemblyPath = (Resolve-Path -LiteralPath $InstallerAssemblyPath).Path
    $assembly = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
    $resourceNames = @($assembly.GetManifestResourceNames() |
        Where-Object { $_.EndsWith(".ReleaseIntegrityPublicKey.pem", [System.StringComparison]::Ordinal) })
    if ($resourceNames.Count -ne 1) {
        throw "El ensamblado del instalador no contiene ReleaseIntegrityPublicKey.pem."
    }
    $resourceName = $resourceNames[0]

    $stream = $assembly.GetManifestResourceStream($resourceName)
    $reader = [System.IO.StreamReader]::new($stream, [System.Text.Encoding]::ASCII)
    try { $embeddedPem = $reader.ReadToEnd() } finally { $reader.Dispose(); $stream.Dispose() }
    if ($embeddedPem.Trim() -ne [System.IO.File]::ReadAllText($publicKeyPath).Trim()) {
        throw "La clave publica incrustada no coincide con el archivo versionado."
    }

    $validationType = $assembly.GetType("NeoTwitch.Installer.ReleaseTrustValidation", $true)
    $validationType.GetMethod("ValidateProduction").Invoke($null, @())
}

Write-Host "Release trust root valida y embebida." -ForegroundColor Green
