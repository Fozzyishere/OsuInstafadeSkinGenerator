using System.Text.RegularExpressions;
using OsuInstaFadeSkinGenerator.Domain;

namespace OsuInstaFadeSkinGenerator.Tests;

internal static class SkinIniTemplateFixture
{
    private const string TemplatesDirectoryName = "skinini-test-template";
    private const string ColoursSection = "[colours]";
    private const string FontsSection = "[fonts]";

    private static readonly Regex ComboLineRegex = new(@"^Combo\d+\s*:", RegexOptions.IgnoreCase);

    public static readonly IReadOnlyDictionary<int, ExpectedSkinConfig> Expected = new Dictionary<int, ExpectedSkinConfig>
    {
        [1] = new(
            Name: "-         《CK》 WhiteCat 2.1 ~ new",
            Author: "cyperdark",
            Version: "2.5",
            HitCircleOverlayAboveNumber: true,
            ComboColours:
            [
                (1, new RgbColor(206, 188, 178)),
                (2, new RgbColor(237, 221, 213)),
            ],
            SliderBorder: new RgbColor(80, 80, 80),
            SliderTrackOverride: new RgbColor(0, 0, 0),
            HitCirclePrefix: "default",
            HitCircleOverlap: 15),
        [2] = new(
            Name: "- JesusOmega {NM} 『Planets』 -",
            Author: "JesusOmega",
            Version: "latest",
            HitCircleOverlayAboveNumber: true,
            ComboColours:
            [
                (1, new RgbColor(255, 105, 125)),
                (2, new RgbColor(224, 177, 252)),
                (3, new RgbColor(131, 180, 252)),
                (4, new RgbColor(88, 196, 112)),
            ],
            SliderBorder: new RgbColor(205, 192, 236),
            SliderTrackOverride: new RgbColor(10, 10, 10),
            HitCirclePrefix: "default",
            HitCircleOverlap: 26),
        [3] = new(
            Name: "BubbleSkin-EditCoquis v2",
            Author: "Various",
            Version: "2.0",
            HitCircleOverlayAboveNumber: true,
            ComboColours:
            [
                (1, new RgbColor(128, 131, 253)),
                (2, new RgbColor(130, 253, 207)),
                (3, new RgbColor(15, 177, 255)),
            ],
            SliderBorder: new RgbColor(70, 70, 70),
            SliderTrackOverride: new RgbColor(0, 0, 20),
            HitCirclePrefix: "default",
            HitCircleOverlap: 10),
        [4] = new(
            Name: "-         《CK》 Bacon boi 1.0",
            Author: "cyperdark",
            Version: "2.5",
            HitCircleOverlayAboveNumber: false,
            ComboColours:
            [
                (1, new RgbColor(241, 214, 207)),
                (2, new RgbColor(206, 137, 137)),
            ],
            SliderBorder: new RgbColor(113, 102, 98),
            SliderTrackOverride: new RgbColor(20, 18, 17),
            HitCirclePrefix: "default",
            HitCircleOverlap: 25),
    };

    public static string GetTemplateContent(int templateNumber)
    {
        return File.ReadAllText(GetTemplatePath(templateNumber)).ReplaceLineEndings(Environment.NewLine);
    }

    public static string GetTemplateContentWithHitCirclePrefix(int templateNumber, string hitCirclePrefix)
    {
        return ReplaceHitCirclePrefix(GetTemplateContent(templateNumber), hitCirclePrefix, templateNumber);
    }

    public static void WriteTemplateSkinIni(string skinFolder, int templateNumber)
    {
        SkinTestHelper.WriteSkinIni(skinFolder, GetTemplateContent(templateNumber));
    }

    public static ExpectedSkinConfig GetExpected(int templateNumber) => Expected[templateNumber];

    public static void AssertSupportedFieldsMatch(int templateNumber, SkinConfig actual)
    {
        var expected = Expected[templateNumber];

        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Author, actual.Author);
        Assert.Equal(expected.Version, actual.Version);
        Assert.Equal(expected.HitCircleOverlayAboveNumber, actual.HitCircleOverlayAboveNumber);
        Assert.Equal(expected.HitCirclePrefix, actual.HitCirclePrefix);
        Assert.Equal(expected.HitCircleOverlap, actual.HitCircleOverlap);
        Assert.Equal(expected.SliderBorder, actual.SliderBorder);
        Assert.Equal(expected.SliderTrackOverride, actual.SliderTrackOverride);

        Assert.Equal(expected.ComboColours.Count, actual.ComboColours.Count);
        foreach (var (index, color) in expected.ComboColours)
        {
            var match = Assert.Single(actual.ComboColours, combo => combo.Index == index);
            Assert.Equal(color, match.Color);
        }
    }

    public static void AssertUpdatedSkinIni(int templateNumber, string updatedContent, string comboValue, int hitCircleOverlap)
    {
        var originalContent = GetTemplateContent(templateNumber);

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

    private static int CountSectionHeaders(string content, string sectionName)
    {
        return EnumerateLines(content)
            .Count(line => line.Trim().Equals($"[{sectionName}]", StringComparison.OrdinalIgnoreCase));
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

    private static string ReplaceHitCirclePrefix(string content, string hitCirclePrefix, int templateNumber)
    {
        var lines = EnumerateLines(content).ToArray();
        var currentSection = string.Empty;
        var replacementCount = 0;
        var prefixLinePattern = new Regex(@"^(\s*HitCirclePrefix\s*:\s*).*$", RegexOptions.IgnoreCase);

        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (TryGetSectionName(trimmed, out var sectionName))
            {
                currentSection = sectionName;
                continue;
            }

            if (currentSection != FontsSection)
            {
                continue;
            }

            var match = prefixLinePattern.Match(lines[i]);
            if (!match.Success)
            {
                continue;
            }

            lines[i] = $"{match.Groups[1].Value}{hitCirclePrefix}";
            replacementCount++;
        }

        return replacementCount switch
        {
            1 => string.Join(Environment.NewLine, lines),
            0 => throw new InvalidOperationException(
                $"Template {templateNumber} does not contain a HitCirclePrefix line in the [Fonts] section."),
            _ => throw new InvalidOperationException(
                $"Template {templateNumber} contains multiple HitCirclePrefix lines in the [Fonts] section."),
        };
    }
}

internal sealed record ExpectedSkinConfig(
    string Name,
    string Author,
    string Version,
    bool HitCircleOverlayAboveNumber,
    IReadOnlyList<(int Index, RgbColor Color)> ComboColours,
    RgbColor? SliderBorder,
    RgbColor? SliderTrackOverride,
    string HitCirclePrefix,
    int HitCircleOverlap);
