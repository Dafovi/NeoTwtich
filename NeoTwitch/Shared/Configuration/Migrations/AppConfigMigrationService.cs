using System.Text.Json;
using NeoTwitch.Models;
using NeoTwitch.Services.Configuration;
using NeoTwitch.Shared;

namespace NeoTwitch.Services.Configuration.Migrations;

public sealed class UnsupportedConfigurationSchemaException : InvalidOperationException
{
    public UnsupportedConfigurationSchemaException(int schemaVersion)
        : base($"La configuración usa el esquema {schemaVersion}; esta versión admite hasta el esquema {AppConfig.CurrentSchemaVersion}.")
    {
        SchemaVersion = schemaVersion;
    }

    public int SchemaVersion { get; }
}

public sealed record AppConfigMigrationResult(
    AppConfig Config,
    int SourceSchemaVersion,
    bool WasMigrated);

public static class AppConfigMigrationService
{
    public static AppConfigMigrationResult DeserializeAndMigrate(
        string json,
        JsonSerializerOptions jsonOptions,
        Func<string>? idFactory = null)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("La raíz de la configuración debe ser un objeto JSON.");
        }

        var sourceVersion = ReadSchemaVersion(document.RootElement);
        if (sourceVersion < 0)
        {
            throw new JsonException("schemaVersion no puede ser negativo.");
        }

        if (sourceVersion > AppConfig.CurrentSchemaVersion)
        {
            throw new UnsupportedConfigurationSchemaException(sourceVersion);
        }

        var config = JsonSerializer.Deserialize<AppConfig>(json, jsonOptions)
            ?? throw new JsonException("La configuración está vacía.");

        var version = sourceVersion;
        while (version < AppConfig.CurrentSchemaVersion)
        {
            version = version switch
            {
                0 => MigrateLegacyToSchema1(config, idFactory ?? (() => Guid.NewGuid().ToString("N"))),
                1 => MigrateSchema1ToSchema2(config),
                _ => throw new InvalidOperationException($"No existe una migración desde el esquema {version}.")
            };
        }

        config.SchemaVersion = AppConfig.CurrentSchemaVersion;
        return new AppConfigMigrationResult(config, sourceVersion, sourceVersion != AppConfig.CurrentSchemaVersion);
    }

    private static int ReadSchemaVersion(JsonElement root)
    {
        int? schemaVersion = null;
        foreach (var property in root.EnumerateObject())
        {
            if (!string.Equals(property.Name, "schemaVersion", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (schemaVersion is not null)
            {
                throw new JsonException("La configuración contiene schemaVersion más de una vez.");
            }

            if (!property.Value.TryGetInt32(out var version))
            {
                throw new JsonException("schemaVersion debe ser un número entero.");
            }

            schemaVersion = version;
        }

        return schemaVersion ?? 0;
    }

    private static int MigrateLegacyToSchema1(AppConfig config, Func<string> idFactory)
    {
        config.Rules ??= [];
        config.AudioLibrary ??= [];
        config.AudioGroups ??= [];
        foreach (var rule in config.Rules)
        {
            AppConfigNormalizer.MigrateLegacyObsMedia(rule);
        }

        AppConfigNormalizer.MigrateRuleAudioLibrary(config, idFactory);
        config.SchemaVersion = 1;
        config.ProtectedSecrets ??= new ProtectedConfigurationSecrets();
        return 1;
    }

    private static int MigrateSchema1ToSchema2(AppConfig config)
    {
        // OAuth tokens belong to the client application that issued them. Switch old local app registrations
        // to Neo Twitch's public desktop client and require a single clean authorization.
        if (!string.Equals(config.TwitchClientId, NeoTwitchProduct.TwitchClientId, StringComparison.Ordinal))
        {
            config.Token = new TwitchTokenInfo();
            config.Channel = new TwitchChannelInfo();
        }

        config.TwitchClientId = NeoTwitchProduct.TwitchClientId;
        config.TwitchClientSecret = "";
        config.ProtectedSecrets ??= new ProtectedConfigurationSecrets();
        config.ProtectedSecrets.TwitchClientSecret = "";
        config.SchemaVersion = 2;
        return 2;
    }
}
