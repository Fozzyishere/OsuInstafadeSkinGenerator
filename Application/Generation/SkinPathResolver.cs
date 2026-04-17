using System.IO;
using OsuInstaFadeSkinGenerator.Application.Ports;
using OsuInstaFadeSkinGenerator.Models;

namespace OsuInstaFadeSkinGenerator.Application.Generation;

internal static class SkinPathResolver
{
    public static string ResolvePrefixPath(string skinFolder, string prefix, string numberSuffix, string hdSuffix)
    {
        var normalized = prefix.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        return Path.Combine(skinFolder, $"{normalized}-{numberSuffix}{hdSuffix}.png");
    }

    public static void EnsureParentDirectory(string filePath, IFileSystem fileSystem)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            ResilientFileOperations.Run(
                () => fileSystem.CreateDirectory(directory),
                GenerationError.IoFailure,
                $"create folder for {GetDisplayPath(filePath)}");
        }
    }

    public static string GetDisplayPath(string path)
    {
        return Path.GetFileName(path) is { Length: > 0 } fileName ? fileName : path;
    }
}
