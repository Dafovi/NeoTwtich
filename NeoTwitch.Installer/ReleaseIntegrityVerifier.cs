using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NeoTwitch.Shared;

namespace NeoTwitch.Installer;

internal enum ReleaseIntegrityFailure
{
    ManifestMissing,
    ManifestMalformed,
    SignatureMissing,
    SignatureInvalid,
    PublicKeyUnavailable,
    UnsupportedSchema,
    WrongProduct,
    VersionMismatch,
    ArtifactMissing,
    ArtifactSizeMismatch,
    ArtifactHashMismatch,
    MalformedHash,
    DuplicateArtifact,
    DownloadFailure
}

internal sealed class ReleaseIntegrityException : InvalidOperationException
{
    public ReleaseIntegrityException(ReleaseIntegrityFailure failure, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Failure = failure;
    }

    public ReleaseIntegrityFailure Failure { get; }
}

internal sealed record TrustedReleaseArtifact(string FileName, byte[] Sha256, long? Size);

internal sealed record TrustedReleaseManifest(string Version, TrustedReleaseArtifact Artifact);

internal sealed class ReleaseIntegrityVerifier
{
    private const int SupportedSchemaVersion = 1;
    private readonly string? _publicKeyPem;

    public ReleaseIntegrityVerifier(string? publicKeyPem)
    {
        _publicKeyPem = publicKeyPem;
    }

    public static ReleaseIntegrityVerifier CreateProduction()
        => new(ReleaseTrustStore.LoadPublicKeyPem());

    public TrustedReleaseManifest VerifyManifest(
        byte[]? manifestBytes,
        byte[]? signatureBytes,
        string releaseVersion,
        string artifactName)
    {
        if (manifestBytes is null || manifestBytes.Length == 0)
        {
            throw Failure(ReleaseIntegrityFailure.ManifestMissing, "El release no contiene el manifiesto de integridad.");
        }

        if (signatureBytes is null || signatureBytes.Length == 0)
        {
            throw Failure(ReleaseIntegrityFailure.SignatureMissing, "El release no contiene la firma del manifiesto.");
        }

        VerifyManifestSignature(manifestBytes, signatureBytes);

        ReleaseIntegrityManifestDto manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<ReleaseIntegrityManifestDto>(manifestBytes)
                ?? throw new JsonException("El documento está vacío.");
        }
        catch (JsonException ex)
        {
            throw Failure(ReleaseIntegrityFailure.ManifestMalformed, "El manifiesto de integridad no es JSON válido.", ex);
        }

        if (manifest.SchemaVersion != SupportedSchemaVersion)
        {
            throw Failure(
                ReleaseIntegrityFailure.UnsupportedSchema,
                $"El esquema {manifest.SchemaVersion} del manifiesto no es compatible.");
        }

        if (!string.Equals(manifest.Product, NeoTwitchProduct.ProductIdentifier, StringComparison.Ordinal))
        {
            throw Failure(ReleaseIntegrityFailure.WrongProduct, "El manifiesto no pertenece a Neo Twitch.");
        }

        var trustedVersion = NeoTwitchProduct.NormalizeVersionText(manifest.Version);
        var discoveredVersion = NeoTwitchProduct.NormalizeVersionText(releaseVersion);
        if (string.IsNullOrWhiteSpace(manifest.Version)
            || !string.Equals(trustedVersion, discoveredVersion, StringComparison.OrdinalIgnoreCase))
        {
            throw Failure(
                ReleaseIntegrityFailure.VersionMismatch,
                $"La versión del manifiesto ({manifest.Version ?? "vacía"}) no coincide con el release ({releaseVersion}).");
        }

        if (manifest.Artifacts is null)
        {
            throw Failure(ReleaseIntegrityFailure.ManifestMalformed, "El manifiesto no contiene una lista de artefactos.");
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in manifest.Artifacts)
        {
            if (candidate is null || string.IsNullOrWhiteSpace(candidate.File))
            {
                throw Failure(ReleaseIntegrityFailure.ManifestMalformed, "El manifiesto contiene un artefacto sin nombre.");
            }

            if (!string.Equals(candidate.File, Path.GetFileName(candidate.File), StringComparison.Ordinal))
            {
                throw Failure(ReleaseIntegrityFailure.ManifestMalformed, "El manifiesto contiene un nombre de artefacto no válido.");
            }

            if (!names.Add(candidate.File))
            {
                throw Failure(
                    ReleaseIntegrityFailure.DuplicateArtifact,
                    $"El manifiesto contiene el artefacto duplicado '{candidate.File}'.");
            }
        }

        var artifact = manifest.Artifacts.SingleOrDefault(candidate =>
            candidate is not null && string.Equals(candidate.File, artifactName, StringComparison.Ordinal));
        if (artifact is null)
        {
            throw Failure(
                ReleaseIntegrityFailure.ArtifactMissing,
                $"El artefacto '{artifactName}' no aparece en el manifiesto firmado.");
        }

        var expectedHash = ParseSha256(artifact.Sha256);
        if (artifact.Size is < 0)
        {
            throw Failure(ReleaseIntegrityFailure.ManifestMalformed, "El tamaño del artefacto no puede ser negativo.");
        }

        return new TrustedReleaseManifest(
            trustedVersion,
            new TrustedReleaseArtifact(artifact.File!, expectedHash, artifact.Size));
    }

    public async Task VerifyArtifactAsync(
        string artifactPath,
        TrustedReleaseArtifact artifact,
        CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(artifactPath);
        if (!fileInfo.Exists)
        {
            throw Failure(ReleaseIntegrityFailure.ArtifactMissing, $"No se descargó '{artifact.FileName}'.");
        }

        if (artifact.Size is long expectedSize && fileInfo.Length != expectedSize)
        {
            throw Failure(
                ReleaseIntegrityFailure.ArtifactSizeMismatch,
                $"El tamaño de '{artifact.FileName}' no coincide con el manifiesto firmado.");
        }

        await using var stream = new FileStream(
            artifactPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var actualHash = await SHA256.HashDataAsync(stream, cancellationToken);
        if (!CryptographicOperations.FixedTimeEquals(artifact.Sha256, actualHash))
        {
            throw Failure(
                ReleaseIntegrityFailure.ArtifactHashMismatch,
                $"El hash SHA-256 de '{artifact.FileName}' no coincide con el manifiesto firmado.");
        }
    }

    private void VerifyManifestSignature(byte[] manifestBytes, byte[] signatureBytes)
    {
        if (string.IsNullOrWhiteSpace(_publicKeyPem))
        {
            throw Failure(
                ReleaseIntegrityFailure.PublicKeyUnavailable,
                "El instalador no contiene una clave pública de releases configurada; la actualización se rechazó.");
        }

        byte[] signature;
        try
        {
            signature = Convert.FromBase64String(Encoding.ASCII.GetString(signatureBytes).Trim());
        }
        catch (FormatException ex)
        {
            throw Failure(ReleaseIntegrityFailure.SignatureInvalid, "La firma del manifiesto tiene un formato inválido.", ex);
        }

        try
        {
            using var verifier = ECDsa.Create();
            verifier.ImportFromPem(_publicKeyPem);
            if (verifier.KeySize != 256)
            {
                throw Failure(ReleaseIntegrityFailure.SignatureInvalid, "La clave pública no usa ECDSA P-256.");
            }

            if (!verifier.VerifyData(
                    manifestBytes,
                    signature,
                    HashAlgorithmName.SHA256,
                    DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
            {
                throw Failure(ReleaseIntegrityFailure.SignatureInvalid, "La firma del manifiesto no es válida.");
            }
        }
        catch (ReleaseIntegrityException)
        {
            throw;
        }
        catch (CryptographicException ex)
        {
            throw Failure(ReleaseIntegrityFailure.SignatureInvalid, "No se pudo validar la firma del manifiesto.", ex);
        }
    }

    private static byte[] ParseSha256(string? hash)
    {
        if (hash is null || hash.Length != 64 || !hash.All(Uri.IsHexDigit))
        {
            throw Failure(ReleaseIntegrityFailure.MalformedHash, "El hash SHA-256 del artefacto es inválido.");
        }

        try
        {
            return Convert.FromHexString(hash);
        }
        catch (FormatException ex)
        {
            throw Failure(ReleaseIntegrityFailure.MalformedHash, "El hash SHA-256 del artefacto es inválido.", ex);
        }
    }

    private static ReleaseIntegrityException Failure(
        ReleaseIntegrityFailure failure,
        string message,
        Exception? innerException = null) =>
        new(failure, message, innerException);

    private sealed record ReleaseIntegrityManifestDto(
        [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
        [property: JsonPropertyName("product")] string? Product,
        [property: JsonPropertyName("version")] string? Version,
        [property: JsonPropertyName("artifacts")] IReadOnlyList<ReleaseArtifactDto?>? Artifacts);

    private sealed record ReleaseArtifactDto(
        [property: JsonPropertyName("file")] string? File,
        [property: JsonPropertyName("sha256")] string? Sha256,
        [property: JsonPropertyName("size")] long? Size);
}

public static class ReleaseTrustValidation
{
    public static void ValidateProduction()
    {
        _ = ReleaseIntegrityVerifier.CreateProduction();
        var publicKeyPem = ReleaseTrustStore.LoadPublicKeyPem();
        if (string.IsNullOrWhiteSpace(publicKeyPem))
        {
            throw new InvalidOperationException("El instalador no contiene la clave pública de producción.");
        }

        using var verifier = ECDsa.Create();
        verifier.ImportFromPem(publicKeyPem);
        if (verifier.KeySize != 256)
        {
            throw new CryptographicException("La clave pública incrustada no usa ECDSA P-256.");
        }
    }
}

internal static class ReleaseTrustStore
{
    private const string PublicKeyResourceSuffix = ".ReleaseIntegrityPublicKey.pem";

    public static string? LoadPublicKeyPem()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .SingleOrDefault(name => name.EndsWith(PublicKeyResourceSuffix, StringComparison.Ordinal));
        if (resourceName is null)
        {
            return null;
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var reader = stream is null ? null : new StreamReader(stream, Encoding.ASCII);
        return reader?.ReadToEnd();
    }
}
