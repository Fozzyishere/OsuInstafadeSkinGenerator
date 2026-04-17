using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OsuInstaFadeSkinGenerator.Application.Generation.Steps;
using OsuInstaFadeSkinGenerator.Application.Ports;
using OsuInstaFadeSkinGenerator.Domain;

namespace OsuInstaFadeSkinGenerator.Application.Generation;

public sealed class InstaFadeGenerationOrchestrator : IGenerationService
{
    private readonly ISkinIniReader skinIniReader;
    private readonly ISkinIniWriter skinIniWriter;
    private readonly IFileSystem fileSystem;
    private readonly IImageIo imageIo;
    private readonly ILogger<InstaFadeGenerationOrchestrator> logger;

    public InstaFadeGenerationOrchestrator(
        ISkinIniReader skinIniReader,
        ISkinIniWriter skinIniWriter,
        IFileSystem fileSystem,
        IImageIo imageIo,
        ILogger<InstaFadeGenerationOrchestrator> logger)
    {
        this.skinIniReader = skinIniReader;
        this.skinIniWriter = skinIniWriter;
        this.fileSystem = fileSystem;
        this.imageIo = imageIo;
        this.logger = logger;
    }

    public async Task<GenerationOutcome> GenerateAsync(
        GenerationRequest request,
        IProgress<GenerationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await this.RunPipelineAsync(request, progress, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return new GenerationOutcome(GenerationStatus.Cancelled, null, "Generation cancelled.");
        }
        catch (GenerationFailureException ex)
        {
            return new GenerationOutcome(GenerationStatus.Failed, ex.Error, ex.Message);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Generation failed unexpectedly.");
            return new GenerationOutcome(GenerationStatus.Failed, GenerationError.Unexpected, $"Generation failed unexpectedly: {ex.Message}");
        }
    }

    private async Task<GenerationOutcome> RunPipelineAsync(
        GenerationRequest request,
        IProgress<GenerationProgress>? progress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var skinFolder = request.SkinFolderPath;
        var skinIniPath = Path.Combine(skinFolder, SkinAssetNames.SkinIni);

        if (!this.fileSystem.DirectoryExists(skinFolder))
        {
            return new GenerationOutcome(GenerationStatus.Failed, GenerationError.SkinFolderMissing, "Skin folder does not exist.");
        }

        if (!this.fileSystem.FileExists(skinIniPath))
        {
            return new GenerationOutcome(GenerationStatus.Failed, GenerationError.SkinIniMissing, "skin.ini not found in skin folder.");
        }

        this.Report(progress, GenerationPhase.ReadingIni, PhaseWeights.ReadingIniStart, "Reading skin.ini...");
        var config = await ResilientFileOperations.RunAsync(
            () => this.skinIniReader.ReadAsync(skinIniPath, cancellationToken),
            GenerationError.IoFailure,
            "read skin.ini").ConfigureAwait(false);
        var prefix = config.HitCirclePrefix;

        this.Report(progress, GenerationPhase.ReadingIni, PhaseWeights.ReadingIniSkinMeta, $"  Skin: {config.Name} (v{config.Version}) by {config.Author}");
        this.Report(progress, GenerationPhase.ReadingIni, PhaseWeights.ReadingIniPrefix, $"  HitCirclePrefix: {prefix}");
        this.Report(progress, GenerationPhase.ReadingIni, PhaseWeights.ReadingIniOverlay, $"  OverlayAboveNumber: {config.HitCircleOverlayAboveNumber}");

        if (request.BackupFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.Report(progress, GenerationPhase.CreatingBackup, PhaseWeights.BackupStart, "Creating backup...");
            await BackupStep.RunAsync(skinFolder, prefix, this.fileSystem, cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
        this.Report(progress, GenerationPhase.ProcessingSd, PhaseWeights.SdStart, "Processing SD images...");
        var sdOutcome = await VariantProcessingStep.RunAsync(
            skinFolder,
            prefix,
            string.Empty,
            config,
            request,
            progress,
            new ProgressRange(PhaseWeights.SdStart, PhaseWeights.SdEnd),
            GenerationPhase.ProcessingSd,
            this.fileSystem,
            this.imageIo,
            cancellationToken).ConfigureAwait(false);
        if (sdOutcome.Status != GenerationStatus.Succeeded)
        {
            return sdOutcome;
        }

        bool hdProcessed = false;
        if (request.ProcessHd)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.Report(progress, GenerationPhase.ProcessingHd, PhaseWeights.HdStart, "Processing HD (@2x) images...");
            var missingHdAssets = HdPrerequisiteCheck.FindMissingHdAssets(skinFolder, this.fileSystem);
            if (missingHdAssets.Count > 0)
            {
                foreach (var missingAsset in missingHdAssets)
                {
                    this.Report(
                        progress,
                        GenerationPhase.ProcessingHd,
                        PhaseWeights.HdStart,
                        $"Warning: Missing required HD asset: {missingAsset}",
                        GenerationError.MissingHdAsset);
                }

                this.Report(
                    progress,
                    GenerationPhase.ProcessingHd,
                    PhaseWeights.HdStart,
                    "Warning: Skipping HD generation and continuing with SD only.",
                    GenerationError.MissingHdAsset);
            }
            else
            {
                var hdOutcome = await VariantProcessingStep.RunAsync(
                    skinFolder,
                    prefix,
                    SkinAssetNames.HdSuffix,
                    config,
                    request,
                    progress,
                    new ProgressRange(PhaseWeights.HdStart, PhaseWeights.HdEnd),
                    GenerationPhase.ProcessingHd,
                    this.fileSystem,
                    this.imageIo,
                    cancellationToken).ConfigureAwait(false);
                if (hdOutcome.Status != GenerationStatus.Succeeded)
                {
                    return hdOutcome;
                }

                hdProcessed = true;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        this.Report(progress, GenerationPhase.UpdatingIni, PhaseWeights.UpdatingIniStart, "Updating skin.ini...");
        var overlapWidth = await SkinIniUpdateStep.ComputeOverlapWidthAsync(
            skinFolder,
            prefix,
            hdProcessed,
            this.fileSystem,
            this.imageIo,
            cancellationToken).ConfigureAwait(false);

        await SkinIniUpdateStep.RunAsync(
            skinIniPath,
            request.ComboColor,
            overlapWidth > 0 ? overlapWidth : 0,
            this.skinIniWriter,
            cancellationToken).ConfigureAwait(false);

        this.Report(progress, GenerationPhase.Done, PhaseWeights.Done, "Done!");
        return new GenerationOutcome(GenerationStatus.Succeeded, null, "Insta-fade hitcircles generated successfully.");
    }

    private void Report(
        IProgress<GenerationProgress>? progress,
        GenerationPhase phase,
        double fraction,
        string message,
        GenerationError? warning = null)
    {
        progress?.Report(new GenerationProgress(phase, fraction, message, warning));
    }
}
