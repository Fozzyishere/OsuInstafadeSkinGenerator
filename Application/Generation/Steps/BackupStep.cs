using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OsuInstaFadeSkinGenerator.Application.Ports;
using OsuInstaFadeSkinGenerator.Domain;

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

    public static async Task<bool> StageAsync(
        string skinFolder,
        string prefix,
        GenerationTransaction transaction,
        IFileSystem fileSystem,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        var backupDir = Path.Combine(skinFolder, SkinAssetNames.BackupFolder);

        foreach (var name in OriginalAssetNames)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            var src = Path.Combine(skinFolder, name);
            if (fileSystem.FileExists(src))
            {
                await StageCopyToBackupAsync(fileSystem, transaction, src, Path.Combine(backupDir, name))
                    .ConfigureAwait(false);
            }
        }

        foreach (var hdSuffix in VariantSuffixes)
        {
            for (int i = 0; i <= 9; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                var src = SkinPathResolver.ResolvePrefixPath(skinFolder, prefix, i.ToString(), hdSuffix);
                if (!fileSystem.FileExists(src))
                {
                    continue;
                }

                var destName = Path.GetFileName(src);
                await StageCopyToBackupAsync(fileSystem, transaction, src, Path.Combine(backupDir, destName))
                    .ConfigureAwait(false);
            }
        }

        var iniSrc = Path.Combine(skinFolder, SkinAssetNames.SkinIni);
        if (fileSystem.FileExists(iniSrc))
        {
            await StageCopyToBackupAsync(fileSystem, transaction, iniSrc, Path.Combine(backupDir, SkinAssetNames.SkinIni))
                .ConfigureAwait(false);
        }

        return !cancellationToken.IsCancellationRequested;
    }

    private static async Task StageCopyToBackupAsync(
        IFileSystem fileSystem,
        GenerationTransaction transaction,
        string sourcePath,
        string destinationPath)
    {
        var stagedPath = transaction.CreateStagedPathForTarget(destinationPath);
        await ResilientFileOperations.RunAsync(
            () => fileSystem.CopyFileAtomicallyAsync(sourcePath, stagedPath, CancellationToken.None),
            GenerationError.IoFailure,
            $"stage backup for {GetDisplayPath(sourcePath)}").ConfigureAwait(false);
    }

    private static string GetDisplayPath(string path)
    {
        return Path.GetFileName(path) is { Length: > 0 } fileName ? fileName : path;
    }
}
