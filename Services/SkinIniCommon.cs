using System.Text.RegularExpressions;

namespace OsuInstaFadeSkinGenerator.Services;

internal static partial class SkinIniCommon
{
    internal const string GeneralSection = "[general]";
    internal const string ColoursSection = "[colours]";
    internal const string FontsSection = "[fonts]";

    internal static bool TryParseRgb(string value, out byte r, out byte g, out byte b)
    {
        r = g = b = 0;
        var parts = value.Split(',');
        return parts.Length >= 3
            && byte.TryParse(parts[0].Trim(), out r)
            && byte.TryParse(parts[1].Trim(), out g)
            && byte.TryParse(parts[2].Trim(), out b);
    }

    internal static string NormalizeKey(string key) => key.ToLowerInvariant();

    internal static bool TryGetSectionName(string line, out string sectionName)
    {
        sectionName = string.Empty;
        if (!line.StartsWith('[') || !line.EndsWith(']'))
        {
            return false;
        }

        sectionName = line.ToLowerInvariant();
        return true;
    }

    internal static string DetectIndent(IReadOnlyList<string> lines)
    {
        foreach (var line in lines)
        {
            if (line.Length > 0 && line[0] is ' ' or '\t' && line.Trim().Contains(':'))
            {
                return GetLineIndent(line);
            }
        }

        return string.Empty;
    }

    internal static string GetLineIndent(string line)
    {
        int i = 0;
        while (i < line.Length && line[i] is ' ' or '\t')
        {
            i++;
        }

        return line[..i];
    }

    [GeneratedRegex(@"^Combo(\d+)$", RegexOptions.IgnoreCase)]
    internal static partial Regex ComboRegex();

    [GeneratedRegex(@"^Combo\d+\s*:", RegexOptions.IgnoreCase)]
    internal static partial Regex ComboLineRegex();
}
