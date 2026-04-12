using OsuInstaFadeSkinGenerator.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;

namespace OsuInstaFadeSkinGenerator.Services;

public sealed class InstaFadeGenerator : IGenerationService
{
    private const string SkinIniFileName = "skin.ini";
    private const string BackupFolderName = "_insta-fade-backup";
    private static readonly string[] OriginalAssetNames =
    [
        "hitcircle.png",
        "hitcircle@2x.png",
        "hitcircleoverlay.png",
        "hitcircleoverlay@2x.png",
    ];

    private static readonly string[] VariantSuffixes = [string.Empty, "@2x"];
    private readonly ISkinIniReader skinIniReader;
    private readonly ISkinIniWriter skinIniWriter;

    public InstaFadeGenerator(ISkinIniReader skinIniReader, ISkinIniWriter skinIniWriter)
    {
        this.skinIniReader = skinIniReader;
        this.skinIniWriter = skinIniWriter;
    }

    public Task<GenerationResult> GenerateAsync(
        GenerationRequest request,
        IProgress<GenerationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => this.Generate(request, progress, cancellationToken));
    }

    private GenerationResult Generate(
        GenerationRequest request,
        IProgress<GenerationProgress>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var skinFolder = request.SkinFolderPath;
            var skinIniPath = Path.Combine(skinFolder, SkinIniFileName);

            if (!Directory.Exists(skinFolder))
            {
                return new GenerationResult(false, "Skin folder does not exist.");
            }

            if (!File.Exists(skinIniPath))
            {
                return new GenerationResult(false, "skin.ini not found in skin folder.");
            }

            this.ReportProgress(progress, 0.0, "Reading skin.ini...");
            var config = this.ParseSkinIniOrThrow(skinIniPath);
            var prefix = config.HitCirclePrefix;

            this.ReportProgress(progress, 0.02, $"  Skin: {config.Name} (v{config.Version}) by {config.Author}");
            this.ReportProgress(progress, 0.03, $"  HitCirclePrefix: {prefix}");
            this.ReportProgress(progress, 0.04, $"  OverlayAboveNumber: {config.HitCircleOverlayAboveNumber}");

            if (request.BackupFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                this.ReportProgress(progress, 0.05, "Creating backup...");
                this.CreateBackup(skinFolder, prefix);
            }

            cancellationToken.ThrowIfCancellationRequested();
            this.ReportProgress(progress, 0.1, "Processing SD images...");
            var sdResult = this.ProcessVariant(
                skinFolder,
                prefix,
                string.Empty,
                config,
                request,
                progress,
                cancellationToken,
                new ProgressRange(0.1, 0.5));
            if (!sdResult.Success)
            {
                return sdResult;
            }

            bool hdProcessed = false;
            if (request.ProcessHd)
            {
                cancellationToken.ThrowIfCancellationRequested();
                this.ReportProgress(progress, 0.5, "Processing HD (@2x) images...");
                var missingHdAssets = this.GetMissingRequiredHdAssets(skinFolder);
                if (missingHdAssets.Count > 0)
                {
                    foreach (var missingAsset in missingHdAssets)
                    {
                        this.ReportProgress(progress, 0.5, $"Warning: Missing required HD asset: {missingAsset}");
                    }

                    this.ReportProgress(progress, 0.5, "Warning: Skipping HD generation and continuing with SD only.");
                }
                else
                {
                    var hdResult = this.ProcessVariant(
                        skinFolder,
                        prefix,
                        "@2x",
                        config,
                        request,
                        progress,
                        cancellationToken,
                        new ProgressRange(0.5, 0.9));
                    if (!hdResult.Success)
                    {
                        return hdResult;
                    }

                    hdProcessed = true;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            this.ReportProgress(progress, 0.9, "Updating skin.ini...");
            var overlapWidth = this.GetDefaultWidth(skinFolder, prefix, string.Empty);
            if (overlapWidth <= 0 && hdProcessed)
            {
                overlapWidth = this.GetDefaultWidth(skinFolder, prefix, "@2x") / 2;
            }

            this.UpdateSkinIniOrThrow(
                skinIniPath,
                request.ComboR,
                request.ComboG,
                request.ComboB,
                overlapWidth > 0 ? overlapWidth : 0);

            this.ReportProgress(progress, 1.0, "Done!");
            return new GenerationResult(true, "Insta-fade hitcircles generated successfully.");
        }
        catch (OperationCanceledException)
        {
            return new GenerationResult(false, "Generation cancelled.");
        }
        catch (GenerationFailureException ex)
        {
            return new GenerationResult(false, ex.Message);
        }
        catch (Exception ex)
        {
            return new GenerationResult(false, $"Generation failed unexpectedly: {ex.Message}");
        }
    }

    private GenerationResult ProcessVariant(
        string skinFolder,
        string prefix,
        string suffix,
        SkinConfig config,
        GenerationRequest request,
        IProgress<GenerationProgress>? progress,
        CancellationToken cancellationToken,
        ProgressRange progressRange)
    {
        var hitcirclePath = Path.Combine(skinFolder, $"hitcircle{suffix}.png");
        var overlayPath = Path.Combine(skinFolder, $"hitcircleoverlay{suffix}.png");

        if (!File.Exists(hitcirclePath))
        {
            if (suffix == "@2x")
            {
                this.ReportProgress(progress, progressRange.Start, "No HD (@2x) found, skipping...");
                return new GenerationResult(true, "No HD (@2x) found, skipping...");
            }

            return new GenerationResult(false, $"hitcircle{suffix}.png not found.");
        }

        using var hitcircle = this.LoadImageOrThrow(hitcirclePath);
        using var overlay = File.Exists(overlayPath)
            ? this.LoadImageOrThrow(overlayPath)
            : ImageProcessor.CreateBlank(hitcircle.Width, hitcircle.Height);

        var progressSpan = progressRange.End - progressRange.Start;
        this.ReportProgress(progress, progressRange.Start + (progressSpan * 0.1), $"Upscaling {suffix}...");

        using var upscaledHitcircle = ImageProcessor.Upscale(hitcircle, 1.25f);
        using var upscaledOverlay = ImageProcessor.Upscale(overlay, 1.25f);

        cancellationToken.ThrowIfCancellationRequested();
        this.ReportProgress(progress, progressRange.Start + (progressSpan * 0.2), $"Tinting {suffix}...");
        ImageProcessor.Tint(upscaledHitcircle, request.ComboR, request.ComboG, request.ComboB);

        cancellationToken.ThrowIfCancellationRequested();
        this.ReportProgress(progress, progressRange.Start + (progressSpan * 0.3), $"Compositing {suffix}...");
        using var merged = ImageProcessor.Composite(upscaledHitcircle, upscaledOverlay);

        for (int i = 1; i <= 9; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var numberPath = this.ResolvePrefixPath(skinFolder, prefix, i.ToString(), suffix);
            this.ReportProgress(
                progress,
                progressRange.Start + (progressSpan * (0.3 + (0.05 * i))),
                $"Processing {Path.GetFileName(numberPath)}...");

            this.EnsureParentDirectory(numberPath);

            if (File.Exists(numberPath))
            {
                using var numberImage = this.LoadImageOrThrow(numberPath);
                using var result = config.HitCircleOverlayAboveNumber
                    ? ImageProcessor.ComposeNumberBetween(upscaledHitcircle, numberImage, upscaledOverlay)
                    : ImageProcessor.PlaceNumberOnCircle(merged, numberImage);
                this.SaveImageAsPngOrThrow(result, numberPath);
            }
            else
            {
                using var clone = merged.Clone();
                this.SaveImageAsPngOrThrow(clone, numberPath);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        var blankPath = this.ResolvePrefixPath(skinFolder, prefix, "0", suffix);
        this.ReportProgress(
            progress,
            progressRange.Start + (progressSpan * 0.85),
            $"Creating blank {Path.GetFileName(blankPath)}...");
        this.EnsureParentDirectory(blankPath);
        using var blank = ImageProcessor.CreateBlank(merged.Width, merged.Height);
        this.SaveImageAsPngOrThrow(blank, blankPath);

        cancellationToken.ThrowIfCancellationRequested();
        this.ReportProgress(progress, progressRange.Start + (progressSpan * 0.95), $"Replacing originals {suffix}...");

        if (request.EnableTripleStacking)
        {
            using var stackedBaseAssets = ImageProcessor.Composite(hitcircle, overlay);
            this.SaveImageAsPngOrThrow(stackedBaseAssets, hitcirclePath);
            this.SaveImageAsPngOrThrow(stackedBaseAssets, overlayPath);
        }
        else
        {
            using var transparentHitcircle = ImageProcessor.CreateBlank(hitcircle.Width, hitcircle.Height);
            this.SaveImageAsPngOrThrow(transparentHitcircle, hitcirclePath);

            using var transparentOverlay = ImageProcessor.CreateBlank(overlay.Width, overlay.Height);
            this.SaveImageAsPngOrThrow(transparentOverlay, overlayPath);
        }

        return new GenerationResult(true, $"Variant {suffix} processed successfully.");
    }

    private string ResolvePrefixPath(string skinFolder, string prefix, string numberSuffix, string hdSuffix)
    {
        var normalized = prefix.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        return Path.Combine(skinFolder, $"{normalized}-{numberSuffix}{hdSuffix}.png");
    }

    private int GetDefaultWidth(string skinFolder, string prefix, string suffix)
    {
        var path = this.ResolvePrefixPath(skinFolder, prefix, "1", suffix);
        if (!File.Exists(path))
        {
            return 0;
        }

        using var img = this.LoadImageOrThrow(path);
        return img.Width;
    }

    private List<string> GetMissingRequiredHdAssets(string skinFolder)
    {
        var requiredPaths = new List<string>
        {
            Path.Combine(skinFolder, "hitcircle@2x.png"),
        };

        return requiredPaths
            .Where(path => !File.Exists(path))
            .Select(path => Path.GetRelativePath(skinFolder, path))
            .ToList();
    }

    private void CreateBackup(string skinFolder, string prefix)
    {
        var backupDir = Path.Combine(skinFolder, BackupFolderName);
        this.CreateDirectoryOrThrow(backupDir, $"create backup folder {this.GetDisplayPath(backupDir)}");

        foreach (var name in OriginalAssetNames)
        {
            var src = Path.Combine(skinFolder, name);
            if (File.Exists(src))
            {
                this.CopyFileOrThrow(src, Path.Combine(backupDir, name));
            }
        }

        foreach (var hdSuffix in VariantSuffixes)
        {
            for (int i = 0; i <= 9; i++)
            {
                var src = this.ResolvePrefixPath(skinFolder, prefix, i.ToString(), hdSuffix);
                if (!File.Exists(src))
                {
                    continue;
                }

                var destName = Path.GetFileName(src);
                this.CopyFileOrThrow(src, Path.Combine(backupDir, destName));
            }
        }

        var iniSrc = Path.Combine(skinFolder, SkinIniFileName);
        if (File.Exists(iniSrc))
        {
            this.CopyFileOrThrow(iniSrc, Path.Combine(backupDir, SkinIniFileName));
        }
    }

    private void EnsureParentDirectory(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            this.CreateDirectoryOrThrow(directory, $"create folder for {this.GetDisplayPath(filePath)}");
        }
    }

    private SkinConfig ParseSkinIniOrThrow(string skinIniPath)
    {
        try
        {
            return this.skinIniReader.Read(skinIniPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new GenerationFailureException("Failed to read skin.ini. Access was denied.", ex);
        }
        catch (PathTooLongException ex)
        {
            throw new GenerationFailureException("Failed to read skin.ini. The path is too long.", ex);
        }
        catch (IOException ex)
        {
            throw new GenerationFailureException("Failed to read skin.ini. The file is in use or unreadable.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new GenerationFailureException("Failed to read skin.ini. The file path is invalid.", ex);
        }
    }

    private void UpdateSkinIniOrThrow(string skinIniPath, byte comboR, byte comboG, byte comboB, int hitCircleOverlap)
    {
        try
        {
            this.skinIniWriter.Update(skinIniPath, comboR, comboG, comboB, hitCircleOverlap);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new GenerationFailureException("Failed to update skin.ini. Access was denied.", ex);
        }
        catch (PathTooLongException ex)
        {
            throw new GenerationFailureException("Failed to update skin.ini. The path is too long.", ex);
        }
        catch (IOException ex)
        {
            throw new GenerationFailureException("Failed to update skin.ini. The file is in use or could not be written.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new GenerationFailureException("Failed to update skin.ini. The file path is invalid.", ex);
        }
    }

    private void ReportProgress(IProgress<GenerationProgress>? progress, double value, string message)
    {
        progress?.Report(new GenerationProgress(value, message));
    }

    private Image<Rgba32> LoadImageOrThrow(string path)
    {
        try
        {
            return Image.Load<Rgba32>(path);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new GenerationFailureException($"Failed to load {this.GetDisplayPath(path)}. Access was denied.", ex);
        }
        catch (PathTooLongException ex)
        {
            throw new GenerationFailureException($"Failed to load {this.GetDisplayPath(path)}. The path is too long.", ex);
        }
        catch (IOException ex)
        {
            throw new GenerationFailureException($"Failed to load {this.GetDisplayPath(path)}. The file is in use or unreadable.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new GenerationFailureException($"Failed to load {this.GetDisplayPath(path)}. The file path is invalid.", ex);
        }
        catch (UnknownImageFormatException ex)
        {
            throw new GenerationFailureException($"Failed to load {this.GetDisplayPath(path)}. The image format is not supported.", ex);
        }
        catch (InvalidImageContentException ex)
        {
            throw new GenerationFailureException($"Failed to load {this.GetDisplayPath(path)}. The image data is invalid or corrupted.", ex);
        }
        catch (NotSupportedException ex)
        {
            throw new GenerationFailureException($"Failed to load {this.GetDisplayPath(path)}. The image format is not supported.", ex);
        }
    }

    private void SaveImageAsPngOrThrow(Image<Rgba32> image, string path)
    {
        try
        {
            image.SaveAsPng(path);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new GenerationFailureException($"Failed to write {this.GetDisplayPath(path)}. Access was denied.", ex);
        }
        catch (PathTooLongException ex)
        {
            throw new GenerationFailureException($"Failed to write {this.GetDisplayPath(path)}. The path is too long.", ex);
        }
        catch (IOException ex)
        {
            throw new GenerationFailureException($"Failed to write {this.GetDisplayPath(path)}. The file is in use or could not be written.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new GenerationFailureException($"Failed to write {this.GetDisplayPath(path)}. The file path is invalid.", ex);
        }
    }

    private void CopyFileOrThrow(string sourcePath, string destinationPath)
    {
        try
        {
            File.Copy(sourcePath, destinationPath, overwrite: true);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new GenerationFailureException($"Failed to back up {this.GetDisplayPath(sourcePath)}. Access was denied.", ex);
        }
        catch (PathTooLongException ex)
        {
            throw new GenerationFailureException($"Failed to back up {this.GetDisplayPath(sourcePath)}. The path is too long.", ex);
        }
        catch (IOException ex)
        {
            throw new GenerationFailureException($"Failed to back up {this.GetDisplayPath(sourcePath)}. The file is in use or could not be copied.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new GenerationFailureException($"Failed to back up {this.GetDisplayPath(sourcePath)}. The file path is invalid.", ex);
        }
    }

    private void CreateDirectoryOrThrow(string directoryPath, string operation)
    {
        try
        {
            Directory.CreateDirectory(directoryPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new GenerationFailureException($"Failed to {operation}. Access was denied.", ex);
        }
        catch (PathTooLongException ex)
        {
            throw new GenerationFailureException($"Failed to {operation}. The path is too long.", ex);
        }
        catch (IOException ex)
        {
            throw new GenerationFailureException($"Failed to {operation}. The folder could not be created.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new GenerationFailureException($"Failed to {operation}. The folder path is invalid.", ex);
        }
        catch (NotSupportedException ex)
        {
            throw new GenerationFailureException($"Failed to {operation}. The folder path is not supported.", ex);
        }
    }

    private string GetDisplayPath(string path)
    {
        return Path.GetFileName(path) is { Length: > 0 } fileName ? fileName : path;
    }

    private readonly record struct ProgressRange(double Start, double End);
}
