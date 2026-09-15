using System.IO;
using NeoTwitch.Models.Library;

namespace NeoTwitch.Services.Library;

public sealed record LibraryPathRelinkResult(int RelinkedCount, int UnresolvedCount, int AmbiguousCount);

public static class LibraryPathRelinkService
{
    public static LibraryPathRelinkResult RelinkMissingAssets(
        IEnumerable<ILibraryAssetConfig> assets,
        string mediaRoot,
        Func<string, bool>? fileExists = null,
        Func<string, IEnumerable<string>>? enumerateFiles = null,
        Func<string, bool>? directoryExists = null)
    {
        fileExists ??= File.Exists;
        enumerateFiles ??= path => Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
        directoryExists ??= Directory.Exists;

        var missingAssets = assets
            .Where(asset => !string.IsNullOrWhiteSpace(asset.FilePath) && !fileExists(asset.FilePath))
            .ToArray();
        if (missingAssets.Length == 0 || !directoryExists(mediaRoot))
        {
            return new LibraryPathRelinkResult(0, missingAssets.Length, 0);
        }

        var candidatesByName = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var path in enumerateFiles(mediaRoot))
            {
                var fileName = Path.GetFileName(path);
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    continue;
                }

                if (!candidatesByName.TryGetValue(fileName, out var candidates))
                {
                    candidates = [];
                    candidatesByName[fileName] = candidates;
                }

                candidates.Add(path);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Files that cannot be listed remain unresolved; the user can choose a narrower folder.
        }
        catch (IOException)
        {
            // A folder can change while it is being scanned. Keep valid matches found so far.
        }

        var relinked = 0;
        var ambiguous = 0;
        foreach (var asset in missingAssets)
        {
            var fileName = Path.GetFileName(asset.FilePath);
            if (!candidatesByName.TryGetValue(fileName, out var candidates) || candidates.Count == 0)
            {
                continue;
            }

            if (candidates.Count != 1)
            {
                ambiguous++;
                continue;
            }

            asset.FilePath = candidates[0];
            relinked++;
        }

        return new LibraryPathRelinkResult(relinked, missingAssets.Length - relinked, ambiguous);
    }
}
