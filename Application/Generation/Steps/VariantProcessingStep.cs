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

    public static async Task<VariantProcessingResult> RunAsync(
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
        Func<string, string> resolveOutputPath,
        CancellationToken cancellationToken)
    {
        var hitcircleFileName = GetVariantFileName(SkinAssetNames.Hitcircle, variantSuffix);
        var overlayFileName = GetVariantFileName(SkinAssetNames.HitcircleOverlay, variantSuffix);
        var hitcirclePath = Path.Combine(skinFolder, hitcircleFileName);
        var overlayPath = Path.Combine(skinFolder, overlayFileName);

        if (cancellationToken.IsCancellationRequested)
        {
            return Cancelled();
        }

        if (!fileSystem.FileExists(hitcirclePath))
        {
            if (variantSuffix == SkinAssetNames.HdSuffix)
            {
                Report(progress, phase, progressRange.Start, "No HD (@2x) found, skipping...");
                return new VariantProcessingResult(
                    new GenerationOutcome(GenerationStatus.Succeeded, null, "No HD (@2x) found, skipping..."),
                    0);
            }

            return new VariantProcessingResult(
                new GenerationOutcome(GenerationStatus.Failed, GenerationError.IoFailure, $"{hitcircleFileName} not found."),
                0);
        }

        using var hitcircle = await imageIo.LoadAsync(hitcirclePath, CancellationToken.None).ConfigureAwait(false);
        using var overlay = fileSystem.FileExists(overlayPath)
            ? await imageIo.LoadAsync(overlayPath, CancellationToken.None).ConfigureAwait(false)
            : ImageProcessor.CreateBlank(hitcircle.Width, hitcircle.Height);

        Report(progress, phase, progressRange.Interpolate(PhaseWeights.VariantUpscale), $"Upscaling {variantSuffix}...");

        using var upscaledHitcircle = ImageProcessor.Upscale(hitcircle, InstaFadeUpscale);
        using var upscaledOverlay = ImageProcessor.Upscale(overlay, InstaFadeUpscale);

        if (cancellationToken.IsCancellationRequested)
        {
            return Cancelled();
        }

        Report(progress, phase, progressRange.Interpolate(PhaseWeights.VariantTint), $"Tinting {variantSuffix}...");
        ImageProcessor.Tint(upscaledHitcircle, request.ComboColor.R, request.ComboColor.G, request.ComboColor.B);

        if (cancellationToken.IsCancellationRequested)
        {
            return Cancelled();
        }

        Report(progress, phase, progressRange.Interpolate(PhaseWeights.VariantComposite), $"Compositing {variantSuffix}...");
        using var merged = ImageProcessor.Composite(upscaledHitcircle, upscaledOverlay);

        var generatedDefaultNumberWidth = 0;

        for (int i = 1; i <= 9; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Cancelled();
            }

            var numberPath = SkinPathResolver.ResolvePrefixPath(skinFolder, prefix, i.ToString(), variantSuffix);
            var numberOutputPath = resolveOutputPath(numberPath);
            Report(
                progress,
                phase,
                progressRange.Interpolate(PhaseWeights.VariantNumberBase + (PhaseWeights.VariantNumberStep * i)),
                $"Processing {Path.GetFileName(numberPath)}...");

            SkinPathResolver.EnsureParentDirectory(numberOutputPath, fileSystem);

            if (fileSystem.FileExists(numberPath))
            {
                using var numberImage = await imageIo.LoadAsync(numberPath, CancellationToken.None).ConfigureAwait(false);
                using var result = config.HitCircleOverlayAboveNumber
                    ? ImageProcessor.ComposeNumberBetween(upscaledHitcircle, numberImage, upscaledOverlay)
                    : ImageProcessor.PlaceNumberOnCircle(merged, numberImage);
                if (i == 1)
                {
                    generatedDefaultNumberWidth = result.Width;
                }

                await imageIo.SaveAsPngAsync(result, numberOutputPath, CancellationToken.None).ConfigureAwait(false);
            }
            else
            {
                using var clone = merged.Clone();
                if (i == 1)
                {
                    generatedDefaultNumberWidth = clone.Width;
                }

                await imageIo.SaveAsPngAsync(clone, numberOutputPath, CancellationToken.None).ConfigureAwait(false);
            }
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Cancelled();
        }

        var blankPath = SkinPathResolver.ResolvePrefixPath(skinFolder, prefix, "0", variantSuffix);
        var blankOutputPath = resolveOutputPath(blankPath);
        Report(
            progress,
            phase,
            progressRange.Interpolate(PhaseWeights.VariantBlank),
            $"Creating blank {Path.GetFileName(blankPath)}...");
        SkinPathResolver.EnsureParentDirectory(blankOutputPath, fileSystem);
        using var blank = ImageProcessor.CreateBlank(merged.Width, merged.Height);
        await imageIo.SaveAsPngAsync(blank, blankOutputPath, CancellationToken.None).ConfigureAwait(false);

        if (cancellationToken.IsCancellationRequested)
        {
            return Cancelled();
        }

        Report(progress, phase, progressRange.Interpolate(PhaseWeights.VariantReplace), $"Replacing originals {variantSuffix}...");

        var hitcircleOutputPath = resolveOutputPath(hitcirclePath);
        var overlayOutputPath = resolveOutputPath(overlayPath);
        SkinPathResolver.EnsureParentDirectory(hitcircleOutputPath, fileSystem);
        SkinPathResolver.EnsureParentDirectory(overlayOutputPath, fileSystem);

        if (request.EnableTripleStacking)
        {
            using var stackedBaseAssets = ImageProcessor.Composite(hitcircle, overlay);
            await imageIo.SaveAsPngAsync(stackedBaseAssets, hitcircleOutputPath, CancellationToken.None).ConfigureAwait(false);
            await imageIo.SaveAsPngAsync(stackedBaseAssets, overlayOutputPath, CancellationToken.None).ConfigureAwait(false);
        }
        else
        {
            using var transparentHitcircle = ImageProcessor.CreateBlank(hitcircle.Width, hitcircle.Height);
            await imageIo.SaveAsPngAsync(transparentHitcircle, hitcircleOutputPath, CancellationToken.None).ConfigureAwait(false);

            using var transparentOverlay = ImageProcessor.CreateBlank(overlay.Width, overlay.Height);
            await imageIo.SaveAsPngAsync(transparentOverlay, overlayOutputPath, CancellationToken.None).ConfigureAwait(false);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Cancelled();
        }

        return new VariantProcessingResult(
            new GenerationOutcome(GenerationStatus.Succeeded, null, $"Variant {variantSuffix} processed successfully."),
            generatedDefaultNumberWidth);
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

    private static VariantProcessingResult Cancelled() =>
        new(new GenerationOutcome(GenerationStatus.Cancelled, null, "Generation cancelled."), 0);
}
