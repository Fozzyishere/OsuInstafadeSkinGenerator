using System.Collections.Generic;
using System.IO;
using System.Linq;
using OsuInstaFadeSkinGenerator.Application.Ports;
using OsuInstaFadeSkinGenerator.Models;

namespace OsuInstaFadeSkinGenerator.Application.Generation.Steps;

internal static class HdPrerequisiteCheck
{
    public static IReadOnlyList<string> FindMissingHdAssets(string skinFolder, IFileSystem fileSystem)
    {
        var requiredPaths = new[]
        {
            Path.Combine(skinFolder, SkinAssetNames.WithHd(SkinAssetNames.Hitcircle)),
        };

        return requiredPaths
            .Where(path => !fileSystem.FileExists(path))
            .Select(path => Path.GetRelativePath(skinFolder, path))
            .ToList();
    }
}
