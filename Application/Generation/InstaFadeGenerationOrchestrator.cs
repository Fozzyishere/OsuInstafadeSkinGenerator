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
        if (cancellationToken.IsCancellationRequested)
        {
            return this.Cancelled();
        }

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
            () => this.skinIniReader.ReadAsync(skinIniPath, CancellationToken.None),
            GenerationError.IoFailure,
            "read skin.ini").ConfigureAwait(false);
        var prefix = config.HitCirclePrefix;

        if (cancellationToken.IsCancellationRequested)
        {
            return this.Cancelled();
        }

        this.Report(progress, GenerationPhase.ReadingIni, PhaseWeights.ReadingIniSkinMeta, $"  Skin: {config.Name} (v{config.Version}) by {config.Author}");
        this.Report(progress, GenerationPhase.ReadingIni, PhaseWeights.ReadingIniPrefix, $"  HitCirclePrefix: {prefix}");
        this.Report(progress, GenerationPhase.ReadingIni, PhaseWeights.ReadingIniOverlay, $"  OverlayAboveNumber: {config.HitCircleOverlayAboveNumber}");

        using var transaction = GenerationTransaction.Create(skinFolder, this.fileSystem);

        if (request.BackupFiles)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return this.Cancelled();
            }

            this.Report(progress, GenerationPhase.CreatingBackup, PhaseWeights.BackupStart, "Creating backup...");
            if (!await BackupStep.StageAsync(skinFolder, prefix, transaction, this.fileSystem, cancellationToken).ConfigureAwait(false))
            {
                return this.Cancelled();
            }
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return this.Cancelled();
        }

        this.Report(progress, GenerationPhase.ProcessingSd, PhaseWeights.SdStart, "Processing SD images...");
        var sdResult = await VariantProcessingStep.RunAsync(
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
            transaction.CreateStagedPathForTarget,
            cancellationToken).ConfigureAwait(false);
        if (sdResult.Outcome.Status != GenerationStatus.Succeeded)
        {
            return sdResult.Outcome;
        }

        bool hdProcessed = false;
        var hdDefaultNumberWidth = 0;
        if (request.ProcessHd)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return this.Cancelled();
            }

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
                var hdResult = await VariantProcessingStep.RunAsync(
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
                    transaction.CreateStagedPathForTarget,
                    cancellationToken).ConfigureAwait(false);
                if (hdResult.Outcome.Status != GenerationStatus.Succeeded)
                {
                    return hdResult.Outcome;
                }

                hdProcessed = true;
                hdDefaultNumberWidth = hdResult.GeneratedDefaultNumberWidth;
            }
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return this.Cancelled();
        }

        this.Report(progress, GenerationPhase.UpdatingIni, PhaseWeights.UpdatingIniStart, "Updating skin.ini...");
        var overlapWidth = sdResult.GeneratedDefaultNumberWidth > 0
            ? sdResult.GeneratedDefaultNumberWidth
            : hdProcessed
                ? hdDefaultNumberWidth / 2
                : 0;
        var stagedSkinIniPath = transaction.CreateStagedPathForTarget(skinIniPath);
        await ResilientFileOperations.RunAsync(
            () => this.fileSystem.CopyFileAtomicallyAsync(skinIniPath, stagedSkinIniPath, CancellationToken.None),
            GenerationError.IoFailure,
            "stage skin.ini").ConfigureAwait(false);

        if (cancellationToken.IsCancellationRequested)
        {
            return this.Cancelled();
        }

        await SkinIniUpdateStep.RunAsync(
            stagedSkinIniPath,
            request.ComboColor,
            overlapWidth,
            this.skinIniWriter,
            CancellationToken.None).ConfigureAwait(false);

        if (cancellationToken.IsCancellationRequested)
        {
            return this.Cancelled();
        }

        this.Report(progress, GenerationPhase.UpdatingIni, PhaseWeights.UpdatingIniStart, "Committing generated files...");
        if (!await transaction.CommitAsync(progress, cancellationToken).ConfigureAwait(false))
        {
            return this.Cancelled();
        }

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

    private GenerationOutcome Cancelled() =>
        new(GenerationStatus.Cancelled, null, "Generation cancelled.");
}
