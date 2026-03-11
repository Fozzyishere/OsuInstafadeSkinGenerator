using System;
using System.IO;
using SixLabors.ImageSharp;
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
            var config = SkinIniParser.Parse(skinIniPath);
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

            if (options.ProcessHd)
            {
                progress?.Invoke(0.5, "Processing HD (@2x) images...");
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
            }

            progress?.Invoke(0.9, "Updating skin.ini...");
            int overlapWidth = GetDefaultWidth(skinFolder, prefix, string.Empty);
            if (overlapWidth <= 0 && options.ProcessHd)
            {
                overlapWidth = GetDefaultWidth(skinFolder, prefix, "@2x") / 2;
            }

            SkinIniParser.UpdateSkinIni(
                skinIniPath,
                options.ComboR,
                options.ComboG,
                options.ComboB,
                overlapWidth > 0 ? overlapWidth : 0);

            progress?.Invoke(1.0, "Done!");
            return new GenerationResult(true, "Insta-fade hitcircles generated successfully.");
        }
        catch (Exception ex)
        {
            return new GenerationResult(false, $"Generation failed: {ex.Message}");
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
                return new GenerationResult(true, "No HD hitcircle found, skipping @2x.");
            }

            return new GenerationResult(false, $"hitcircle{suffix}.png not found.");
        }

        using var hitcircle = Image.Load<Rgba32>(hitcirclePath);

        using var overlay = File.Exists(overlayPath)
            ? Image.Load<Rgba32>(overlayPath)
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
                using var numberImage = Image.Load<Rgba32>(numberPath);
                using var result = config.HitCircleOverlayAboveNumber
                    ? ImageProcessor.ComposeNumberBetween(upscaledHitcircle, numberImage, upscaledOverlay)
                    : ImageProcessor.PlaceNumberOnCircle(merged, numberImage);
                result.SaveAsPng(numberPath);
            }
            else
            {
                using var clone = merged.Clone();
                clone.SaveAsPng(numberPath);
            }
        }

        var blankPath = ResolvePrefixPath(skinFolder, prefix, "0", suffix);
        progress?.Invoke(
            progressRange.Start + (progressSpan * 0.85),
            $"Creating blank {Path.GetFileName(blankPath)}...");
        EnsureParentDirectory(blankPath);
        using var blank = ImageProcessor.CreateBlank(merged.Width, merged.Height);
        blank.SaveAsPng(blankPath);

        progress?.Invoke(progressRange.Start + (progressSpan * 0.95), $"Replacing originals {suffix}...");

        if (options.EnableTripleStacking)
        {
            overlay.SaveAsPng(hitcirclePath);
            overlay.SaveAsPng(overlayPath);
        }
        else
        {
            using var transparentHc = ImageProcessor.CreateBlank(hitcircle.Width, hitcircle.Height);
            transparentHc.SaveAsPng(hitcirclePath);

            using var transparentOverlay = ImageProcessor.CreateBlank(overlay.Width, overlay.Height);
            transparentOverlay.SaveAsPng(overlayPath);
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

        using var img = Image.Load<Rgba32>(path);
        return img.Width;
    }

    private static void CreateBackup(string skinFolder, string prefix)
    {
        var backupDir = Path.Combine(skinFolder, BackupFolderName);
        Directory.CreateDirectory(backupDir);

        foreach (var name in OriginalAssetNames)
        {
            var src = Path.Combine(skinFolder, name);
            if (File.Exists(src))
            {
                File.Copy(src, Path.Combine(backupDir, name), overwrite: true);
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
                File.Copy(src, Path.Combine(backupDir, destName), overwrite: true);
            }
        }

        var iniSrc = Path.Combine(skinFolder, SkinIniFileName);
        if (File.Exists(iniSrc))
        {
            File.Copy(iniSrc, Path.Combine(backupDir, SkinIniFileName), overwrite: true);
        }
    }

    private static void EnsureParentDirectory(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
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
