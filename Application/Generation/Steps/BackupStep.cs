using System;
using System.Globalization;
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
        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            var fullSkinFolder = Path.GetFullPath(skinFolder);
            var backupDir = Path.Combine(fullSkinFolder, SkinAssetNames.BackupFolder, CreateBackupRunFolderName());

            foreach (var name in OriginalAssetNames)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                var src = Path.Combine(skinFolder, name);
                if (fileSystem.FileExists(src))
                {
                    await StageCopyToBackupAsync(fileSystem, transaction, src, Path.Combine(backupDir, name), cancellationToken)
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

                    var destPath = Path.Combine(
                        backupDir,
                        Path.GetRelativePath(fullSkinFolder, Path.GetFullPath(src)));
                    await StageCopyToBackupAsync(fileSystem, transaction, src, destPath, cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            var iniSrc = Path.Combine(skinFolder, SkinAssetNames.SkinIni);
            if (fileSystem.FileExists(iniSrc))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                await StageCopyToBackupAsync(fileSystem, transaction, iniSrc, Path.Combine(backupDir, SkinAssetNames.SkinIni), cancellationToken)
                    .ConfigureAwait(false);
            }

            return !cancellationToken.IsCancellationRequested;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private static async Task StageCopyToBackupAsync(
        IFileSystem fileSystem,
        GenerationTransaction transaction,
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        var stagedPath = transaction.CreateStagedPathForTarget(destinationPath);
        await ResilientFileOperations.RunAsync(
            () => fileSystem.CopyFileAtomicallyAsync(sourcePath, stagedPath, cancellationToken),
            GenerationError.IoFailure,
            $"stage backup for {GetDisplayPath(sourcePath)}").ConfigureAwait(false);
    }

    private static string GetDisplayPath(string path)
    {
        return Path.GetFileName(path) is { Length: > 0 } fileName ? fileName : path;
    }

    private static string CreateBackupRunFolderName()
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
        return $"{timestamp}-{Guid.NewGuid():N}";
    }
}
