using System.IO;

namespace OsuInstaFadeSkinGenerator.Application.Generation;

internal static class SkinPathResolver
{
    public static string ResolvePrefixPath(string skinFolder, string prefix, string numberSuffix, string hdSuffix)
    {
        var normalized = prefix.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        return Path.Combine(skinFolder, $"{normalized}-{numberSuffix}{hdSuffix}.png");
    }

    public static void EnsureParentDirectory(string filePath, Ports.IFileSystem fileSystem)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Infrastructure.Io.ResilientFileOperations.Run(
                () => fileSystem.CreateDirectory(directory),
                Models.GenerationError.IoFailure,
                $"create folder for {GetDisplayPath(filePath)}");
        }
    }

    public static string GetDisplayPath(string path)
    {
        return Path.GetFileName(path) is { Length: > 0 } fileName ? fileName : path;
    }
}
