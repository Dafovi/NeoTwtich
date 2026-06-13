using System.IO;

namespace NeoTwitch.Models.Library;

public interface ILibraryAssetConfig
{
    string Id { get; set; }

    string Name { get; set; }

    string FilePath { get; set; }

    string GroupId { get; set; }

    DateTimeOffset? LastUsedAt { get; set; }

    string DisplayName => !string.IsNullOrWhiteSpace(Name)
        ? Name
        : string.IsNullOrWhiteSpace(FilePath)
            ? "Archivo sin nombre"
            : Path.GetFileNameWithoutExtension(FilePath);
}
