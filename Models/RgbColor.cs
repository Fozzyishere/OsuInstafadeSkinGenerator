using System;
using System.Globalization;

namespace OsuInstaFadeSkinGenerator.Models;

public readonly record struct RgbColor(byte R, byte G, byte B)
{
    public string Hex => $"#{this.R:X2}{this.G:X2}{this.B:X2}";

    public static bool TryParseHex(string? input, out RgbColor color)
    {
        color = default;

        var hex = input?.Trim() ?? string.Empty;
        if (!hex.StartsWith('#'))
        {
            hex = "#" + hex;
        }

        if (hex.Length == 7
            && byte.TryParse(hex[1..3], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var red)
            && byte.TryParse(hex[3..5], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var green)
            && byte.TryParse(hex[5..7], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var blue))
        {
            color = new RgbColor(red, green, blue);
            return true;
        }

        return false;
    }

    public static bool TryParseTriplet(string? redText, string? greenText, string? blueText, out RgbColor color)
    {
        color = default;

        if (byte.TryParse(redText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var red)
            && byte.TryParse(greenText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var green)
            && byte.TryParse(blueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var blue))
        {
            color = new RgbColor(red, green, blue);
            return true;
        }

        return false;
    }

    public static bool TryParseCsv(string? csv, out RgbColor color)
    {
        color = default;

        if (csv is null)
        {
            return false;
        }

        var commentIndex = csv.IndexOf("//", StringComparison.Ordinal);
        var sanitized = (commentIndex >= 0 ? csv[..commentIndex] : csv).TrimEnd();
        var parts = sanitized.Split(',');
        if (parts.Length < 3)
        {
            return false;
        }

        return TryParseTriplet(parts[0].Trim(), parts[1].Trim(), parts[2].Trim(), out color);
    }
}
