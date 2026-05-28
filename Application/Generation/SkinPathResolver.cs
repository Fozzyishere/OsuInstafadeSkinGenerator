using System;
using System.IO;
using OsuInstaFadeSkinGenerator.Application.Ports;
using OsuInstaFadeSkinGenerator.Domain;

namespace OsuInstaFadeSkinGenerator.Application.Generation;

internal static class SkinPathResolver
{
    public static string ResolvePrefixPath(string skinFolder, string prefix, string numberSuffix, string hdSuffix)
    {
        var normalized = prefix.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        var resolvedPath = Path.Combine(skinFolder, $"{normalized}-{numberSuffix}{hdSuffix}.png");
        ThrowIfOutsideSkinFolder(
            skinFolder,
            resolvedPath,
            GenerationError.UnsafeOutputPath,
            $"HitCirclePrefix '{prefix}' resolves outside the selected skin folder.");

        return resolvedPath;
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

    internal static void ThrowIfOutsideSkinFolder(string skinFolder, string targetPath, GenerationError error, string message)
    {
        var normalizedSkinFolder = Path.GetFullPath(skinFolder);
        var skinFolderPrefix = Path.EndsInDirectorySeparator(normalizedSkinFolder)
            ? normalizedSkinFolder
            : normalizedSkinFolder + Path.DirectorySeparatorChar;
        var fullTargetPath = Path.GetFullPath(targetPath);

        if (!fullTargetPath.StartsWith(skinFolderPrefix, GetPathComparison()))
        {
            throw new GenerationFailureException(error, message);
        }
    }

    private static StringComparison GetPathComparison() =>
        OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
}
