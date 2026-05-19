using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OsuInstaFadeSkinGenerator.Application.Imaging;
using OsuInstaFadeSkinGenerator.Application.Ports;
using OsuInstaFadeSkinGenerator.Domain;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace OsuInstaFadeSkinGenerator.Application.Generation.Steps;

internal static class VariantProcessingStep
{
    private const float InstaFadeUpscale = 1.25f;

    public static async Task<GenerationOutcome> RunAsync(
        string skinFolder,
        string prefix,
        string variantSuffix,
        SkinConfig config,
        GenerationRequest request,
        IProgress<GenerationProgress>? progress,
        ProgressRange progressRange,
        GenerationPhase phase,
        IFileSystem fileSystem,
        IImageIo imageIo,
        CancellationToken cancellationToken)
    {
        var hitcircleFileName = GetVariantFileName(SkinAssetNames.Hitcircle, variantSuffix);
        var overlayFileName = GetVariantFileName(SkinAssetNames.HitcircleOverlay, variantSuffix);
        var hitcirclePath = Path.Combine(skinFolder, hitcircleFileName);
        var overlayPath = Path.Combine(skinFolder, overlayFileName);

        if (!fileSystem.FileExists(hitcirclePath))
        {
            if (variantSuffix == SkinAssetNames.HdSuffix)
            {
                Report(progress, phase, progressRange.Start, "No HD (@2x) found, skipping...");
                return new GenerationOutcome(GenerationStatus.Succeeded, null, "No HD (@2x) found, skipping...");
            }

            return new GenerationOutcome(GenerationStatus.Failed, GenerationError.IoFailure, $"{hitcircleFileName} not found.");
        }

        using var hitcircle = await imageIo.LoadAsync(hitcirclePath, cancellationToken).ConfigureAwait(false);
        using var overlay = fileSystem.FileExists(overlayPath)
            ? await imageIo.LoadAsync(overlayPath, cancellationToken).ConfigureAwait(false)
            : ImageProcessor.CreateBlank(hitcircle.Width, hitcircle.Height);

        Report(progress, phase, progressRange.Interpolate(PhaseWeights.VariantUpscale), $"Upscaling {variantSuffix}...");

        using var upscaledHitcircle = ImageProcessor.Upscale(hitcircle, InstaFadeUpscale);
        using var upscaledOverlay = ImageProcessor.Upscale(overlay, InstaFadeUpscale);

        cancellationToken.ThrowIfCancellationRequested();
        Report(progress, phase, progressRange.Interpolate(PhaseWeights.VariantTint), $"Tinting {variantSuffix}...");
        ImageProcessor.Tint(upscaledHitcircle, request.ComboColor.R, request.ComboColor.G, request.ComboColor.B);

        cancellationToken.ThrowIfCancellationRequested();
        Report(progress, phase, progressRange.Interpolate(PhaseWeights.VariantComposite), $"Compositing {variantSuffix}...");
        using var merged = ImageProcessor.Composite(upscaledHitcircle, upscaledOverlay);

        cancellationToken.ThrowIfCancellationRequested();
        var nonCancelableToken = CancellationToken.None;

        for (int i = 1; i <= 9; i++)
        {
            var numberPath = SkinPathResolver.ResolvePrefixPath(skinFolder, prefix, i.ToString(), variantSuffix);
            Report(
                progress,
                phase,
                progressRange.Interpolate(PhaseWeights.VariantNumberBase + (PhaseWeights.VariantNumberStep * i)),
                $"Processing {Path.GetFileName(numberPath)}...");

            SkinPathResolver.EnsureParentDirectory(numberPath, fileSystem);

            if (fileSystem.FileExists(numberPath))
            {
                using var numberImage = await imageIo.LoadAsync(numberPath, nonCancelableToken).ConfigureAwait(false);
                using var result = config.HitCircleOverlayAboveNumber
                    ? ImageProcessor.ComposeNumberBetween(upscaledHitcircle, numberImage, upscaledOverlay)
                    : ImageProcessor.PlaceNumberOnCircle(merged, numberImage);
                await imageIo.SaveAsPngAsync(result, numberPath, nonCancelableToken).ConfigureAwait(false);
            }
            else
            {
                using var clone = merged.Clone();
                await imageIo.SaveAsPngAsync(clone, numberPath, nonCancelableToken).ConfigureAwait(false);
            }
        }

        var blankPath = SkinPathResolver.ResolvePrefixPath(skinFolder, prefix, "0", variantSuffix);
        Report(
            progress,
            phase,
            progressRange.Interpolate(PhaseWeights.VariantBlank),
            $"Creating blank {Path.GetFileName(blankPath)}...");
        SkinPathResolver.EnsureParentDirectory(blankPath, fileSystem);
        using var blank = ImageProcessor.CreateBlank(merged.Width, merged.Height);
        await imageIo.SaveAsPngAsync(blank, blankPath, nonCancelableToken).ConfigureAwait(false);

        Report(progress, phase, progressRange.Interpolate(PhaseWeights.VariantReplace), $"Replacing originals {variantSuffix}...");

        if (request.EnableTripleStacking)
        {
            using var stackedBaseAssets = ImageProcessor.Composite(hitcircle, overlay);
            await imageIo.SaveAsPngAsync(stackedBaseAssets, hitcirclePath, nonCancelableToken).ConfigureAwait(false);
            await imageIo.SaveAsPngAsync(stackedBaseAssets, overlayPath, nonCancelableToken).ConfigureAwait(false);
        }
        else
        {
            using var transparentHitcircle = ImageProcessor.CreateBlank(hitcircle.Width, hitcircle.Height);
            await imageIo.SaveAsPngAsync(transparentHitcircle, hitcirclePath, nonCancelableToken).ConfigureAwait(false);

            using var transparentOverlay = ImageProcessor.CreateBlank(overlay.Width, overlay.Height);
            await imageIo.SaveAsPngAsync(transparentOverlay, overlayPath, nonCancelableToken).ConfigureAwait(false);
        }

        return new GenerationOutcome(GenerationStatus.Succeeded, null, $"Variant {variantSuffix} processed successfully.");
    }

    private static void Report(
        IProgress<GenerationProgress>? progress,
        GenerationPhase phase,
        double fraction,
        string message,
        GenerationError? warning = null)
    {
        progress?.Report(new GenerationProgress(phase, fraction, message, warning));
    }

    private static string GetVariantFileName(string baseName, string variantSuffix)
        => variantSuffix == SkinAssetNames.HdSuffix ? SkinAssetNames.WithHd(baseName) : baseName;
}
