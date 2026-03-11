using System.Text.RegularExpressions;
using OsuInstaFadeSkinGenerator.Models;

namespace OsuInstaFadeSkinGenerator.Services;

public static partial class SkinIniParser
{
    private const string GeneralSection = "[general]";
    private const string ColoursSection = "[colours]";
    private const string FontsSection = "[fonts]";

    public static SkinConfig Parse(string skinIniPath)
    {
        var config = new SkinConfig();
        var lines = File.ReadAllLines(skinIniPath);
        string currentSection = string.Empty;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//"))
            {
                continue;
            }

            if (TryGetSectionName(trimmed, out var sectionName))
            {
                currentSection = sectionName;
                continue;
            }

            var colonIdx = trimmed.IndexOf(':');
            if (colonIdx < 0)
            {
                continue;
            }

            var key = trimmed[..colonIdx].Trim();
            var value = trimmed[(colonIdx + 1)..].Trim();

            switch (currentSection)
            {
                case GeneralSection:
                    ParseGeneral(config, key, value);
                    break;
                case ColoursSection:
                    ParseColours(config, key, value);
                    break;
                case FontsSection:
                    ParseFonts(config, key, value);
                    break;
            }
        }

        return config;
    }

    public static void UpdateSkinIni(string skinIniPath, byte comboR, byte comboG, byte comboB, int hitCircleOverlap)
    {
        var lines = File.ReadAllLines(skinIniPath).ToList();
        var result = new List<string>();
        bool combo1Written = false;
        bool overlapWritten = false;
        string currentSection = string.Empty;
        string indent = DetectIndent(lines);
        var comboValue = $"{comboR},{comboG},{comboB}";

        for (int i = 0; i < lines.Count; i++)
        {
            var trimmed = lines[i].Trim();

            if (TryGetSectionName(trimmed, out var sectionName))
            {
                FlushSection(
                    result,
                    currentSection,
                    indent,
                    comboValue,
                    hitCircleOverlap,
                    ref combo1Written,
                    ref overlapWritten);
                currentSection = sectionName;
            }

            if (currentSection == FontsSection && trimmed.StartsWith("HitCircleOverlap", StringComparison.OrdinalIgnoreCase))
            {
                result.Add($"{GetLineIndent(lines[i])}HitCircleOverlap: {hitCircleOverlap}");
                overlapWritten = true;
                continue;
            }

            if (currentSection == ColoursSection && ComboLineRegex().IsMatch(trimmed))
            {
                if (!combo1Written)
                {
                    result.Add($"{GetLineIndent(lines[i])}Combo1: {comboValue}");
                    combo1Written = true;
                }

                continue;
            }

            result.Add(lines[i]);
        }

        FlushSection(
            result,
            currentSection,
            indent,
            comboValue,
            hitCircleOverlap,
            ref combo1Written,
            ref overlapWritten);

        if (!combo1Written)
        {
            AppendNewSection(result, "[Colours]", $"Combo1: {comboValue}", indent);
        }

        if (!overlapWritten)
        {
            AppendNewSection(result, "[Fonts]", $"HitCircleOverlap: {hitCircleOverlap}", indent);
        }

        File.WriteAllLines(skinIniPath, result);
    }

    private static void ParseGeneral(SkinConfig config, string key, string value)
    {
        switch (NormalizeKey(key))
        {
            case "name":
                config.Name = value;
                break;
            case "author":
                config.Author = value;
                break;
            case "version":
                config.Version = value;
                break;

            // taking into account old config
            case "hitcircleoverlayabovenumber":
            case "hitcircleoverlayabovenumer":
                config.HitCircleOverlayAboveNumber = value == "1";
                break;
        }
    }

    private static void ParseColours(SkinConfig config, string key, string value)
    {
        var comboMatch = ComboRegex().Match(key);
        if (comboMatch.Success)
        {
            var index = int.Parse(comboMatch.Groups[1].Value);
            if (TryParseRgb(value, out var r, out var g, out var b))
            {
                config.ComboColours.Add(new ComboColour(index, r, g, b));
            }

            return;
        }

        switch (NormalizeKey(key))
        {
            case "sliderborder":
                if (TryParseRgb(value, out var sbr, out var sbg, out var sbb))
                {
                    config.SliderBorder = new RgbColour(sbr, sbg, sbb);
                }

                break;
            case "slidertrackoverride":
                if (TryParseRgb(value, out var str, out var stg, out var stb))
                {
                    config.SliderTrackOverride = new RgbColour(str, stg, stb);
                }

                break;
        }
    }

    private static void ParseFonts(SkinConfig config, string key, string value)
    {
        switch (NormalizeKey(key))
        {
            case "hitcircleprefix":
                if (!string.IsNullOrEmpty(value))
                {
                    config.HitCirclePrefix = value;
                }

                break;
            case "hitcircleoverlap":
                if (int.TryParse(value, out var overlap))
                {
                    config.HitCircleOverlap = overlap;
                }

                break;
        }
    }

    private static bool TryParseRgb(string value, out byte r, out byte g, out byte b)
    {
        r = g = b = 0;
        var parts = value.Split(',');
        return parts.Length >= 3
            && byte.TryParse(parts[0].Trim(), out r)
            && byte.TryParse(parts[1].Trim(), out g)
            && byte.TryParse(parts[2].Trim(), out b);
    }

    // helper to flush pending lines when changing sections or at eof
    private static void FlushSection(
        List<string> result,
        string section,
        string indent,
        string comboValue,
        int hitCircleOverlap,
        ref bool combo1Written,
        ref bool overlapWritten)
    {
        if (section == FontsSection && !overlapWritten)
        {
            result.Add($"{indent}HitCircleOverlap: {hitCircleOverlap}");
            overlapWritten = true;
        }

        if (section == ColoursSection && !combo1Written)
        {
            result.Add($"{indent}Combo1: {comboValue}");
            combo1Written = true;
        }
    }

    private static void AppendNewSection(List<string> result, string header, string keyLine, string indent)
    {
        result.Add(string.Empty);
        result.Add(header);
        result.Add($"{indent}{keyLine}");
    }

    private static string DetectIndent(List<string> lines)
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

    private static string GetLineIndent(string line)
    {
        int i = 0;
        while (i < line.Length && line[i] is ' ' or '\t')
        {
            i++;
        }

        return line[..i];
    }

    private static string NormalizeKey(string key) => key.ToLowerInvariant();

    private static bool TryGetSectionName(string line, out string sectionName)
    {
        sectionName = string.Empty;
        if (!line.StartsWith('[') || !line.EndsWith(']'))
        {
            return false;
        }

        sectionName = line.ToLowerInvariant();
        return true;
    }

    [GeneratedRegex(@"^Combo(\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex ComboRegex();

    [GeneratedRegex(@"^Combo\d+\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex ComboLineRegex();
}
