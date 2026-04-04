using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;

namespace OsuInstaFadeSkinGenerator.Services;

public static class InstaFadeGenerator
{
    // some glabal skin.ini values we need to read
    private const string SkinIniFileName = "skin.ini";
    private const string BackupFolderName = "_insta-fade-backup";
    private static readonly string[] OriginalAssetNames =
    [
        "hitcircle.png",
        "hitcircle@2x.png",
        "hitcircleoverlay.png",
        "hitcircleoverlay@2x.png"
    ];

    private static readonly string[] VariantSuffixes = [string.Empty, "@2x"];

    public static GenerationResult Generate(GenerationOptions options, Action<double, string>? progress = null)
    {
        try
        {
            var skinFolder = options.SkinFolderPath;
            var skinIniPath = Path.Combine(skinFolder, SkinIniFileName);

            if (!Directory.Exists(skinFolder))
            {
                return new GenerationResult(false, "Skin folder does not exist.");
            }

            if (!File.Exists(skinIniPath))
            {
                return new GenerationResult(false, "skin.ini not found in skin folder.");
            }

            progress?.Invoke(0.0, "Reading skin.ini...");
            var config = ParseSkinIniOrThrow(skinIniPath);
            var prefix = config.HitCirclePrefix;

            progress?.Invoke(0.02, $"  Skin: {config.Name} (v{config.Version}) by {config.Author}");
            progress?.Invoke(0.03, $"  HitCirclePrefix: {prefix}");
            progress?.Invoke(0.04, $"  OverlayAboveNumber: {config.HitCircleOverlayAboveNumber}");

            if (options.BackupFiles)
            {
                progress?.Invoke(0.05, "Creating backup...");
                CreateBackup(skinFolder, prefix);
            }

            progress?.Invoke(0.1, "Processing SD images...");
            var sdResult = ProcessVariant(
                skinFolder,
                prefix,
                string.Empty,
                config,
                options,
                progress,
                new ProgressRange(0.1, 0.5));
            if (!sdResult.Success)
            {
                return sdResult;
            }

            bool hdProcessed = false;
            if (options.ProcessHd)
            {
                progress?.Invoke(0.5, "Processing HD (@2x) images...");
                var missingHdAssets = GetMissingRequiredHdAssets(skinFolder, prefix);
                if (missingHdAssets.Count > 0)
                {
                    foreach (var missingAsset in missingHdAssets)
                    {
                        progress?.Invoke(0.5, $"ERROR: Missing required HD asset: {missingAsset}");
                    }

                    progress?.Invoke(0.5, "ERROR: Skipping HD generation and continuing with SD only.");
                }
                else
                {
                    var hdResult = ProcessVariant(
                        skinFolder,
                        prefix,
                        "@2x",
                        config,
                        options,
                        progress,
                        new ProgressRange(0.5, 0.9));
                    if (!hdResult.Success)
                    {
                        return hdResult;
                    }

                    hdProcessed = true;
                }
            }

            progress?.Invoke(0.9, "Updating skin.ini...");
            int overlapWidth = GetDefaultWidth(skinFolder, prefix, string.Empty);
            if (overlapWidth <= 0 && hdProcessed)
            {
                overlapWidth = GetDefaultWidth(skinFolder, prefix, "@2x") / 2;
            }

            UpdateSkinIniOrThrow(
                skinIniPath,
                options.ComboR,
                options.ComboG,
                options.ComboB,
                overlapWidth > 0 ? overlapWidth : 0);

            progress?.Invoke(1.0, "Done!");
            return new GenerationResult(true, "Insta-fade hitcircles generated successfully.");
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

    private static string ResolvePrefixPath(string skinFolder, string prefix, string numberSuffix, string hdSuffix)
    {
        var normalized = prefix.Replace('/', Path.DirectorySeparatorChar)
                               .Replace('\\', Path.DirectorySeparatorChar);
        return Path.Combine(skinFolder, $"{normalized}-{numberSuffix}{hdSuffix}.png");
    }

    private static GenerationResult ProcessVariant(
        string skinFolder,
        string prefix,
        string suffix,
        Models.SkinConfig config,
        GenerationOptions options,
        Action<double, string>? progress,
        ProgressRange progressRange)
    {
        var hitcirclePath = Path.Combine(skinFolder, $"hitcircle{suffix}.png");
        var overlayPath = Path.Combine(skinFolder, $"hitcircleoverlay{suffix}.png");

        if (!File.Exists(hitcirclePath))
        {
            if (suffix == "@2x")
            {
                progress?.Invoke(progressRange.Start, "No HD (@2x) found, skipping...");
                return new GenerationResult(true, "No HD (@2x) found, skipping...");
            }

            return new GenerationResult(false, $"hitcircle{suffix}.png not found.");
        }

        using var hitcircle = LoadImageOrThrow(hitcirclePath);

        using var overlay = File.Exists(overlayPath)
            ? LoadImageOrThrow(overlayPath)
            : ImageProcessor.CreateBlank(hitcircle.Width, hitcircle.Height);

        var progressSpan = progressRange.End - progressRange.Start;
        progress?.Invoke(progressRange.Start + (progressSpan * 0.1), $"Upscaling {suffix}...");

        using var upscaledHitcircle = ImageProcessor.Upscale(hitcircle, 1.25f);
        using var upscaledOverlay = ImageProcessor.Upscale(overlay, 1.25f);

        progress?.Invoke(progressRange.Start + (progressSpan * 0.2), $"Tinting {suffix}...");
        ImageProcessor.Tint(upscaledHitcircle, options.ComboR, options.ComboG, options.ComboB);

        progress?.Invoke(progressRange.Start + (progressSpan * 0.3), $"Compositing {suffix}...");
        using var merged = ImageProcessor.Composite(upscaledHitcircle, upscaledOverlay);

        for (int i = 1; i <= 9; i++)
        {
            var numberPath = ResolvePrefixPath(skinFolder, prefix, i.ToString(), suffix);
            progress?.Invoke(
                progressRange.Start + (progressSpan * (0.3 + (0.05 * i))),
                $"Processing {Path.GetFileName(numberPath)}...");

            EnsureParentDirectory(numberPath);

            if (File.Exists(numberPath))
            {
                using var numberImage = LoadImageOrThrow(numberPath);
                using var result = config.HitCircleOverlayAboveNumber
                    ? ImageProcessor.ComposeNumberBetween(upscaledHitcircle, numberImage, upscaledOverlay)
                    : ImageProcessor.PlaceNumberOnCircle(merged, numberImage);
                SaveImageAsPngOrThrow(result, numberPath);
            }
            else
            {
                using var clone = merged.Clone();
                SaveImageAsPngOrThrow(clone, numberPath);
            }
        }

        var blankPath = ResolvePrefixPath(skinFolder, prefix, "0", suffix);
        progress?.Invoke(
            progressRange.Start + (progressSpan * 0.85),
            $"Creating blank {Path.GetFileName(blankPath)}...");
        EnsureParentDirectory(blankPath);
        using var blank = ImageProcessor.CreateBlank(merged.Width, merged.Height);
        SaveImageAsPngOrThrow(blank, blankPath);

        progress?.Invoke(progressRange.Start + (progressSpan * 0.95), $"Replacing originals {suffix}...");

        if (options.EnableTripleStacking)
        {
            SaveImageAsPngOrThrow(overlay, hitcirclePath);
            SaveImageAsPngOrThrow(overlay, overlayPath);
        }
        else
        {
            using var transparentHc = ImageProcessor.CreateBlank(hitcircle.Width, hitcircle.Height);
            SaveImageAsPngOrThrow(transparentHc, hitcirclePath);

            using var transparentOverlay = ImageProcessor.CreateBlank(overlay.Width, overlay.Height);
            SaveImageAsPngOrThrow(transparentOverlay, overlayPath);
        }

        return new GenerationResult(true, $"Variant {suffix} processed successfully.");
    }

    private static int GetDefaultWidth(string skinFolder, string prefix, string suffix)
    {
        var path = ResolvePrefixPath(skinFolder, prefix, "1", suffix);
        if (!File.Exists(path))
        {
            return 0;
        }

        using var img = LoadImageOrThrow(path);
        return img.Width;
    }

    private static List<string> GetMissingRequiredHdAssets(string skinFolder, string prefix)
    {
        var requiredPaths = new List<string>
        {
            Path.Combine(skinFolder, "hitcircle@2x.png"),
            Path.Combine(skinFolder, "hitcircleoverlay@2x.png"),
        };

        for (int i = 1; i <= 9; i++)
        {
            requiredPaths.Add(ResolvePrefixPath(skinFolder, prefix, i.ToString(), "@2x"));
        }

        return requiredPaths
            .Where(path => !File.Exists(path))
            .Select(path => Path.GetRelativePath(skinFolder, path))
            .ToList();
    }

    private static void CreateBackup(string skinFolder, string prefix)
    {
        var backupDir = Path.Combine(skinFolder, BackupFolderName);
        CreateDirectoryOrThrow(backupDir, $"create backup folder {GetDisplayPath(backupDir)}");

        foreach (var name in OriginalAssetNames)
        {
            var src = Path.Combine(skinFolder, name);
            if (File.Exists(src))
            {
                CopyFileOrThrow(src, Path.Combine(backupDir, name));
            }
        }

        foreach (var hdSuffix in VariantSuffixes)
        {
            for (int i = 0; i <= 9; i++)
            {
                var src = ResolvePrefixPath(skinFolder, prefix, i.ToString(), hdSuffix);
                if (!File.Exists(src))
                {
                    continue;
                }

                var destName = Path.GetFileName(src);
                CopyFileOrThrow(src, Path.Combine(backupDir, destName));
            }
        }

        var iniSrc = Path.Combine(skinFolder, SkinIniFileName);
        if (File.Exists(iniSrc))
        {
            CopyFileOrThrow(iniSrc, Path.Combine(backupDir, SkinIniFileName));
        }
    }

    private static void EnsureParentDirectory(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            CreateDirectoryOrThrow(directory, $"create folder for {GetDisplayPath(filePath)}");
        }
    }

    private static Models.SkinConfig ParseSkinIniOrThrow(string skinIniPath)
    {
        try
        {
            return SkinIniParser.Parse(skinIniPath);
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

    private static void UpdateSkinIniOrThrow(string skinIniPath, byte comboR, byte comboG, byte comboB, int hitCircleOverlap)
    {
        try
        {
            SkinIniParser.UpdateSkinIni(skinIniPath, comboR, comboG, comboB, hitCircleOverlap);
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

    private static Image<Rgba32> LoadImageOrThrow(string path)
    {
        try
        {
            return Image.Load<Rgba32>(path);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new GenerationFailureException($"Failed to load {GetDisplayPath(path)}. Access was denied.", ex);
        }
        catch (PathTooLongException ex)
        {
            throw new GenerationFailureException($"Failed to load {GetDisplayPath(path)}. The path is too long.", ex);
        }
        catch (IOException ex)
        {
            throw new GenerationFailureException($"Failed to load {GetDisplayPath(path)}. The file is in use or unreadable.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new GenerationFailureException($"Failed to load {GetDisplayPath(path)}. The file path is invalid.", ex);
        }
        catch (UnknownImageFormatException ex)
        {
            throw new GenerationFailureException($"Failed to load {GetDisplayPath(path)}. The image format is not supported.", ex);
        }
        catch (InvalidImageContentException ex)
        {
            throw new GenerationFailureException($"Failed to load {GetDisplayPath(path)}. The image data is invalid or corrupted.", ex);
        }
        catch (NotSupportedException ex)
        {
            throw new GenerationFailureException($"Failed to load {GetDisplayPath(path)}. The image format is not supported.", ex);
        }
    }

    private static void SaveImageAsPngOrThrow(Image<Rgba32> image, string path)
    {
        try
        {
            image.SaveAsPng(path);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new GenerationFailureException($"Failed to write {GetDisplayPath(path)}. Access was denied.", ex);
        }
        catch (PathTooLongException ex)
        {
            throw new GenerationFailureException($"Failed to write {GetDisplayPath(path)}. The path is too long.", ex);
        }
        catch (IOException ex)
        {
            throw new GenerationFailureException($"Failed to write {GetDisplayPath(path)}. The file is in use or could not be written.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new GenerationFailureException($"Failed to write {GetDisplayPath(path)}. The file path is invalid.", ex);
        }
    }

    private static void CopyFileOrThrow(string sourcePath, string destinationPath)
    {
        try
        {
            File.Copy(sourcePath, destinationPath, overwrite: true);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new GenerationFailureException($"Failed to back up {GetDisplayPath(sourcePath)}. Access was denied.", ex);
        }
        catch (PathTooLongException ex)
        {
            throw new GenerationFailureException($"Failed to back up {GetDisplayPath(sourcePath)}. The path is too long.", ex);
        }
        catch (IOException ex)
        {
            throw new GenerationFailureException($"Failed to back up {GetDisplayPath(sourcePath)}. The file is in use or could not be copied.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new GenerationFailureException($"Failed to back up {GetDisplayPath(sourcePath)}. The file path is invalid.", ex);
        }
    }

    private static void CreateDirectoryOrThrow(string directoryPath, string operation)
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

    private static string GetDisplayPath(string path)
    {
        return Path.GetFileName(path) is { Length: > 0 } fileName ? fileName : path;
    }

    private readonly record struct ProgressRange(double Start, double End);

    public record GenerationResult(bool Success, string Message);

    public record GenerationOptions(
        string SkinFolderPath,
        byte ComboR,
        byte ComboG,
        byte ComboB,
        bool ProcessHd,
        bool BackupFiles,
        bool EnableTripleStacking);
}
