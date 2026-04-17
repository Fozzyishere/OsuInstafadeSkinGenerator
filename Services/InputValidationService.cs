using OsuInstaFadeSkinGenerator.Models;

namespace OsuInstaFadeSkinGenerator.Services;

public sealed class InputValidationService : IInputValidationService
{
    public ColourSelection? GetPrimaryComboColour(SkinConfig config)
    {
        var comboColour = config.ComboColours
            .OrderBy(colour => colour.Index)
            .FirstOrDefault();

        return comboColour == null
            ? null
            : new ColourSelection(comboColour.R, comboColour.G, comboColour.B);
    }

    public SkinFolderValidationResult ValidateSkinFolder(string? inputPath, bool requireValue)
    {
        var trimmedPath = inputPath?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedPath))
        {
            return requireValue
                ? SkinFolderValidationResult.Invalid("Enter a valid osu! skin folder path.")
                : SkinFolderValidationResult.Empty;
        }

        try
        {
            var skinFolderPath = Path.GetFullPath(trimmedPath);
            if (!Directory.Exists(skinFolderPath))
            {
                return SkinFolderValidationResult.Invalid("Skin folder path does not exist.");
            }

            var skinIniPath = Path.Combine(skinFolderPath, "skin.ini");
            if (!File.Exists(skinIniPath))
            {
                return SkinFolderValidationResult.Invalid("Selected folder is not an osu! skin folder because skin.ini is missing.");
            }

            return SkinFolderValidationResult.Valid(skinFolderPath, skinIniPath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return SkinFolderValidationResult.Invalid("Skin folder path is not a valid folder path.");
        }
    }

    public ColourValidationResult ValidateColourInput(
        string? redText,
        string? greenText,
        string? blueText,
        string? hexText,
        bool requireValue)
    {
        var hasAnyValue = !string.IsNullOrWhiteSpace(redText)
            || !string.IsNullOrWhiteSpace(greenText)
            || !string.IsNullOrWhiteSpace(blueText)
            || !string.IsNullOrWhiteSpace(hexText);

        if (!hasAnyValue)
        {
            return requireValue
                ? ColourValidationResult.Invalid("Enter a combo colour in RGB or hex.")
                : ColourValidationResult.Empty;
        }

        if (!string.IsNullOrWhiteSpace(hexText) && this.TryParseHex(hexText, out var hexColour))
        {
            return ColourValidationResult.Valid(hexColour);
        }

        if (this.TryParseRgb(redText, greenText, blueText, out var rgbColour))
        {
            return ColourValidationResult.Valid(rgbColour);
        }

        return ColourValidationResult.Invalid("Colour must be specified as RGB (0 to 255) or hex (#RRGGBB).");
    }

    public ColourValidationResult ValidateRgbInput(
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
                ? ColourValidationResult.Invalid("Enter a combo colour in RGB.")
                : ColourValidationResult.Empty;
        }

        return this.TryParseRgb(redText, greenText, blueText, out var colour)
            ? ColourValidationResult.Valid(colour)
            : ColourValidationResult.Invalid("RGB colour must use values from 0 to 255 for R, G, and B.");
    }

    public ColourValidationResult ValidateHexInput(string? hexText, bool requireValue)
    {
        if (string.IsNullOrWhiteSpace(hexText))
        {
            return requireValue
                ? ColourValidationResult.Invalid("Enter a combo colour in hex.")
                : ColourValidationResult.Empty;
        }

        return this.TryParseHex(hexText, out var colour)
            ? ColourValidationResult.Valid(colour)
            : ColourValidationResult.Invalid("Hex colour must use the format #RRGGBB.");
    }

    public bool TryParseRgb(string? redText, string? greenText, string? blueText, out ColourSelection colour)
    {
        colour = default;

        if (byte.TryParse(redText, out var red)
            && byte.TryParse(greenText, out var green)
            && byte.TryParse(blueText, out var blue))
        {
            colour = new ColourSelection(red, green, blue);
            return true;
        }

        return false;
    }

    public bool TryParseHex(string? input, out ColourSelection colour)
    {
        colour = default;

        var hex = input?.Trim() ?? string.Empty;
        if (!hex.StartsWith('#'))
        {
            hex = "#" + hex;
        }

        if (hex.Length == 7
            && byte.TryParse(hex[1..3], System.Globalization.NumberStyles.HexNumber, null, out var red)
            && byte.TryParse(hex[3..5], System.Globalization.NumberStyles.HexNumber, null, out var green)
            && byte.TryParse(hex[5..7], System.Globalization.NumberStyles.HexNumber, null, out var blue))
        {
            colour = new ColourSelection(red, green, blue);
            return true;
        }

        return false;
    }
}
