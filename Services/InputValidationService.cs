using System;
using System.IO;
using System.Linq;
using OsuInstaFadeSkinGenerator.Models;

namespace OsuInstaFadeSkinGenerator.Services;

public sealed class InputValidationService : IInputValidationService
{
    public RgbColor? GetPrimaryComboColour(SkinConfig config)
    {
        if (config.ComboColours.Count == 0)
        {
            return null;
        }

        return config.ComboColours
            .OrderBy(entry => entry.Index)
            .First()
            .Color;
    }

    public SkinFolderValidation ValidateSkinFolder(string? inputPath, bool requireValue)
    {
        var trimmedPath = inputPath?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedPath))
        {
            return requireValue
                ? new SkinFolderValidation.Invalid("Enter a valid osu! skin folder path.")
                : new SkinFolderValidation.Empty();
        }

        try
        {
            var skinFolderPath = Path.GetFullPath(trimmedPath);
            if (!Directory.Exists(skinFolderPath))
            {
                return new SkinFolderValidation.Invalid("Skin folder path does not exist.");
            }

            var skinIniPath = Path.Combine(skinFolderPath, SkinAssetNames.SkinIni);
            if (!File.Exists(skinIniPath))
            {
                return new SkinFolderValidation.Invalid("Selected folder is not an osu! skin folder because skin.ini is missing.");
            }

            return new SkinFolderValidation.Valid(skinFolderPath, skinIniPath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return new SkinFolderValidation.Invalid("Skin folder path is not a valid folder path.");
        }
    }

    public ColourValidation ValidateRgbInput(
        string? redText,
        string? greenText,
        string? blueText,
        bool requireValue)
    {
        var hasAnyValue = !string.IsNullOrWhiteSpace(redText)
            || !string.IsNullOrWhiteSpace(greenText)
            || !string.IsNullOrWhiteSpace(blueText);

        if (!hasAnyValue)
        {
            return requireValue
                ? new ColourValidation.Invalid("Enter a combo colour in RGB.")
                : new ColourValidation.Empty();
        }

        return this.TryParseRgb(redText, greenText, blueText, out var colour)
            ? new ColourValidation.Valid(colour)
            : new ColourValidation.Invalid("RGB colour must use values from 0 to 255 for R, G, and B.");
    }

    public ColourValidation ValidateHexInput(string? hexText, bool requireValue)
    {
        if (string.IsNullOrWhiteSpace(hexText))
        {
            return requireValue
                ? new ColourValidation.Invalid("Enter a combo colour in hex.")
                : new ColourValidation.Empty();
        }

        return this.TryParseHex(hexText, out var colour)
            ? new ColourValidation.Valid(colour)
            : new ColourValidation.Invalid("Hex colour must use the format #RRGGBB.");
    }

    public bool TryParseRgb(string? redText, string? greenText, string? blueText, out RgbColor colour)
        => RgbColor.TryParseTriplet(redText, greenText, blueText, out colour);

    public bool TryParseHex(string? input, out RgbColor colour)
        => RgbColor.TryParseHex(input, out colour);
}
