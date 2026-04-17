using System.Threading;
using System.Threading.Tasks;
using OsuInstaFadeSkinGenerator.Application.Ports;
using OsuInstaFadeSkinGenerator.Domain;

namespace OsuInstaFadeSkinGenerator.Application.Generation.Steps;

internal static class SkinIniUpdateStep
{
    public static async Task<int> ComputeOverlapWidthAsync(
        string skinFolder,
        string prefix,
        bool hdProcessed,
        IFileSystem fileSystem,
        IImageIo imageIo,
        CancellationToken cancellationToken)
    {
        var sdWidth = await GetDefaultWidthAsync(skinFolder, prefix, string.Empty, fileSystem, imageIo, cancellationToken).ConfigureAwait(false);
        if (sdWidth > 0)
        {
            return sdWidth;
        }

        if (!hdProcessed)
        {
            return 0;
        }

        var hdWidth = await GetDefaultWidthAsync(skinFolder, prefix, SkinAssetNames.HdSuffix, fileSystem, imageIo, cancellationToken).ConfigureAwait(false);
        return hdWidth / 2;
    }

    public static Task RunAsync(
        string skinIniPath,
        RgbColor comboColor,
        int hitCircleOverlap,
        ISkinIniWriter writer,
        CancellationToken cancellationToken)
    {
        return ResilientFileOperations.RunAsync(
            () => writer.UpdateAsync(skinIniPath, comboColor, hitCircleOverlap, cancellationToken),
            GenerationError.IoFailure,
            "update skin.ini");
    }

    private static async Task<int> GetDefaultWidthAsync(
        string skinFolder,
        string prefix,
        string suffix,
        IFileSystem fileSystem,
        IImageIo imageIo,
        CancellationToken cancellationToken)
    {
        var path = SkinPathResolver.ResolvePrefixPath(skinFolder, prefix, "1", suffix);
        if (!fileSystem.FileExists(path))
        {
            return 0;
        }

        using var img = await imageIo.LoadAsync(path, cancellationToken).ConfigureAwait(false);
        return img.Width;
    }
}
