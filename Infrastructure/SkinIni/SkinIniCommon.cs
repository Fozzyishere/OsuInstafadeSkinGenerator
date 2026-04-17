using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OsuInstaFadeSkinGenerator.Infrastructure.SkinIni;

internal static partial class SkinIniCommon
{
    internal const string GeneralSection = "[general]";
    internal const string ColoursSection = "[colours]";
    internal const string FontsSection = "[fonts]";

    internal static bool TryParseOsuBoolean(string value, out bool parsedValue)
    {
        switch (TrimInlineComment(value))
        {
            case "0":
                parsedValue = false;
                return true;
            case "1":
                parsedValue = true;
                return true;
            default:
                parsedValue = false;
                return false;
        }
    }

    internal static string TrimInlineComment(string value)
    {
        var commentIndex = value.IndexOf("//", StringComparison.Ordinal);
        return commentIndex >= 0 ? value[..commentIndex].TrimEnd() : value.TrimEnd();
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
