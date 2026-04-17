using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OsuInstaFadeSkinGenerator.Application.Ports;
using OsuInstaFadeSkinGenerator.Infrastructure.Io;
using OsuInstaFadeSkinGenerator.Models;

namespace OsuInstaFadeSkinGenerator.Application.Generation.Steps;

internal static class BackupStep
{
    private static readonly string[] OriginalAssetNames =
    [
        SkinAssetNames.Hitcircle,
        SkinAssetNames.WithHd(SkinAssetNames.Hitcircle),
        SkinAssetNames.HitcircleOverlay,
        SkinAssetNames.WithHd(SkinAssetNames.HitcircleOverlay),
    ];

    private static readonly string[] VariantSuffixes = [string.Empty, SkinAssetNames.HdSuffix];

    public static Task RunAsync(string skinFolder, string prefix, IFileSystem fileSystem, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var backupDir = Path.Combine(skinFolder, SkinAssetNames.BackupFolder);
        ResilientFileOperations.Run(
            () => fileSystem.CreateDirectory(backupDir),
            GenerationError.IoFailure,
            $"create backup folder {GetDisplayPath(backupDir)}");

        foreach (var name in OriginalAssetNames)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var src = Path.Combine(skinFolder, name);
            if (fileSystem.FileExists(src))
            {
                CopyToBackup(fileSystem, src, Path.Combine(backupDir, name));
            }
        }

        foreach (var hdSuffix in VariantSuffixes)
        {
            for (int i = 0; i <= 9; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var src = SkinPathResolver.ResolvePrefixPath(skinFolder, prefix, i.ToString(), hdSuffix);
                if (!fileSystem.FileExists(src))
                {
                    continue;
                }

                var destName = Path.GetFileName(src);
                CopyToBackup(fileSystem, src, Path.Combine(backupDir, destName));
            }
        }

        var iniSrc = Path.Combine(skinFolder, SkinAssetNames.SkinIni);
        if (fileSystem.FileExists(iniSrc))
        {
            CopyToBackup(fileSystem, iniSrc, Path.Combine(backupDir, SkinAssetNames.SkinIni));
        }

        return Task.CompletedTask;
    }

    private static void CopyToBackup(IFileSystem fileSystem, string sourcePath, string destinationPath)
    {
        ResilientFileOperations.Run(
            () => fileSystem.CopyFile(sourcePath, destinationPath, overwrite: true),
            GenerationError.IoFailure,
            $"back up {GetDisplayPath(sourcePath)}");
    }

    private static string GetDisplayPath(string path)
    {
        return Path.GetFileName(path) is { Length: > 0 } fileName ? fileName : path;
    }
}
