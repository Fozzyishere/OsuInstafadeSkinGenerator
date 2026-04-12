using System.Text.RegularExpressions;
using OsuInstaFadeSkinGenerator.Models;

namespace OsuInstaFadeSkinGenerator.Tests;

internal static class SkinIniTemplateFixture
{
    private const string TemplatesDirectoryName = "skinini-test-template";
    private const string GeneralSection = "[general]";
    private const string ColoursSection = "[colours]";
    private const string FontsSection = "[fonts]";

    private static readonly Regex ComboKeyRegex = new(@"^Combo(\d+)$", RegexOptions.IgnoreCase);
    private static readonly Regex ComboLineRegex = new(@"^Combo\d+\s*:", RegexOptions.IgnoreCase);

    public static string GetTemplateContent(int templateNumber)
    {
        return File.ReadAllText(GetTemplatePath(templateNumber)).ReplaceLineEndings(Environment.NewLine);
    }

    public static void WriteTemplateSkinIni(string skinFolder, int templateNumber)
    {
        SkinTestHelper.WriteSkinIni(skinFolder, GetTemplateContent(templateNumber));
    }

    public static SupportedSkinIniValues ParseSupportedFields(string content)
    {
        var expected = new SupportedSkinIniValues();
        string currentSection = string.Empty;

        foreach (var line in EnumerateLines(content))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            if (TryGetSectionName(trimmed, out var sectionName))
            {
                currentSection = sectionName;
                continue;
            }

            var colonIndex = trimmed.IndexOf(':');
            if (colonIndex < 0)
            {
                continue;
            }

            var key = trimmed[..colonIndex].Trim();
            var value = trimmed[(colonIndex + 1)..].Trim();

            switch (currentSection)
            {
                case GeneralSection:
                    ParseGeneral(expected, key, value);
                    break;
                case ColoursSection:
                    ParseColours(expected, key, value);
                    break;
                case FontsSection:
                    ParseFonts(expected, key, value);
                    break;
            }
        }

        return expected;
    }

    public static void AssertSupportedFieldsMatch(string content, SkinConfig actual)
    {
        var expected = ParseSupportedFields(content);

        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Author, actual.Author);
        Assert.Equal(expected.Version, actual.Version);
        Assert.Equal(expected.HitCircleOverlayAboveNumber, actual.HitCircleOverlayAboveNumber);
        Assert.Equal(expected.HitCirclePrefix, actual.HitCirclePrefix);
        Assert.Equal(expected.HitCircleOverlap, actual.HitCircleOverlap);

        Assert.Equal(expected.ComboColours.Count, actual.ComboColours.Count);
        foreach (var expectedCombo in expected.ComboColours)
        {
            var actualCombo = Assert.Single(actual.ComboColours, combo => combo.Index == expectedCombo.Key);
            Assert.Equal(expectedCombo.Value.R, actualCombo.R);
            Assert.Equal(expectedCombo.Value.G, actualCombo.G);
            Assert.Equal(expectedCombo.Value.B, actualCombo.B);
        }

        AssertRgbEquals(expected.SliderBorder, actual.SliderBorder);
        AssertRgbEquals(expected.SliderTrackOverride, actual.SliderTrackOverride);
    }

    public static void AssertUpdatedSkinIni(string originalContent, string updatedContent, string comboValue, int hitCircleOverlap)
    {
        Assert.Contains($"Combo1: {comboValue}", updatedContent);
        Assert.Contains($"HitCircleOverlap: {hitCircleOverlap}", updatedContent);

        var comboLines = updatedContent
            .Split(Environment.NewLine, StringSplitOptions.None)
            .Select(line => line.Trim())
            .Where(line => ComboLineRegex.IsMatch(line))
            .ToList();

        var comboLine = Assert.Single(comboLines);
        Assert.Equal($"Combo1: {comboValue}", comboLine);

        Assert.Equal(
            CountSectionHeaders(originalContent, "Mania"),
            CountSectionHeaders(updatedContent, "Mania"));

        foreach (var preservedLine in GetLinesExpectedToRemainUnchanged(originalContent))
        {
            Assert.Contains(preservedLine, updatedContent);
        }
    }

    private static string GetTemplatePath(int templateNumber)
    {
        return Path.Combine(AppContext.BaseDirectory, TemplatesDirectoryName, $"{templateNumber}.ini");
    }

    private static IEnumerable<string> EnumerateLines(string content)
    {
        return content
            .ReplaceLineEndings(Environment.NewLine)
            .Split(Environment.NewLine, StringSplitOptions.None);
    }

    private static IEnumerable<string> GetLinesExpectedToRemainUnchanged(string content)
    {
        string currentSection = string.Empty;

        foreach (var line in EnumerateLines(content))
        {
            var trimmed = line.Trim();
            if (TryGetSectionName(trimmed, out var sectionName))
            {
                currentSection = sectionName;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (currentSection == ColoursSection && ComboLineRegex.IsMatch(trimmed))
            {
                continue;
            }

            if (currentSection == FontsSection
                && trimmed.StartsWith("HitCircleOverlap", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return line;
        }
    }

    private static int CountSectionHeaders(string content, string sectionName)
    {
        return EnumerateLines(content)
            .Count(line => line.Trim().Equals($"[{sectionName}]", StringComparison.OrdinalIgnoreCase));
    }

    private static void ParseGeneral(SupportedSkinIniValues expected, string key, string value)
    {
        switch (NormalizeKey(key))
        {
            case "name":
                expected.Name = value;
                break;
            case "author":
                expected.Author = value;
                break;
            case "version":
                expected.Version = value;
                break;
            case "hitcircleoverlayabovenumber":
            case "hitcircleoverlayabovenumer":
                if (TryParseOsuBoolean(value, out var hitCircleOverlayAboveNumber))
                {
                    expected.HitCircleOverlayAboveNumber = hitCircleOverlayAboveNumber;
                }

                break;
        }
    }

    private static void ParseColours(SupportedSkinIniValues expected, string key, string value)
    {
        var comboMatch = ComboKeyRegex.Match(key);
        if (comboMatch.Success)
        {
            if (TryParseRgb(value, out var comboColour))
            {
                expected.ComboColours[int.Parse(comboMatch.Groups[1].Value)] = comboColour;
            }

            return;
        }

        switch (NormalizeKey(key))
        {
            case "sliderborder":
                if (TryParseRgb(value, out var sliderBorder))
                {
                    expected.SliderBorder = sliderBorder;
                }

                break;
            case "slidertrackoverride":
                if (TryParseRgb(value, out var sliderTrackOverride))
                {
                    expected.SliderTrackOverride = sliderTrackOverride;
                }

                break;
        }
    }

    private static void ParseFonts(SupportedSkinIniValues expected, string key, string value)
    {
        switch (NormalizeKey(key))
        {
            case "hitcircleprefix":
                if (!string.IsNullOrEmpty(value))
                {
                    expected.HitCirclePrefix = value;
                }

                break;
            case "hitcircleoverlap":
                if (int.TryParse(TrimInlineComment(value), out var hitCircleOverlap))
                {
                    expected.HitCircleOverlap = hitCircleOverlap;
                }

                break;
        }
    }

    private static bool TryParseRgb(string value, out RgbTriplet rgb)
    {
        rgb = default;
        var parts = TrimInlineComment(value).Split(',');
        if (parts.Length < 3
            || !byte.TryParse(parts[0].Trim(), out var r)
            || !byte.TryParse(parts[1].Trim(), out var g)
            || !byte.TryParse(parts[2].Trim(), out var b))
        {
            return false;
        }

        rgb = new RgbTriplet(r, g, b);
        return true;
    }

    private static bool TryParseOsuBoolean(string value, out bool parsedValue)
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

    private static string TrimInlineComment(string value)
    {
        var commentIndex = value.IndexOf("//", StringComparison.Ordinal);
        return commentIndex >= 0 ? value[..commentIndex].TrimEnd() : value.TrimEnd();
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

    private static void AssertRgbEquals(RgbTriplet? expected, RgbColour? actual)
    {
        if (expected is null)
        {
            Assert.Null(actual);
            return;
        }

        Assert.NotNull(actual);
        Assert.Equal(expected.Value.R, actual!.R);
        Assert.Equal(expected.Value.G, actual.G);
        Assert.Equal(expected.Value.B, actual.B);
    }

    internal sealed class SupportedSkinIniValues
    {
        public string Name { get; set; } = "Unknown";

        public string Author { get; set; } = string.Empty;

        public string Version { get; set; } = "1.0";

        public bool HitCircleOverlayAboveNumber { get; set; } = true;

        public Dictionary<int, RgbTriplet> ComboColours { get; } = [];

        public RgbTriplet? SliderBorder { get; set; }

        public RgbTriplet? SliderTrackOverride { get; set; }

        public string HitCirclePrefix { get; set; } = "default";

        public int HitCircleOverlap { get; set; } = -2;
    }

    internal readonly record struct RgbTriplet(byte R, byte G, byte B);
}
