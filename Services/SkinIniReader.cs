using OsuInstaFadeSkinGenerator.Models;

namespace OsuInstaFadeSkinGenerator.Services;

public sealed class SkinIniReader : ISkinIniReader
{
    public SkinConfig Read(string skinIniPath)
    {
        var config = new SkinConfig();
        var lines = File.ReadAllLines(skinIniPath);
        string currentSection = string.Empty;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            if (SkinIniCommon.TryGetSectionName(trimmed, out var sectionName))
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
                case SkinIniCommon.GeneralSection:
                    ParseGeneral(config, key, value);
                    break;
                case SkinIniCommon.ColoursSection:
                    ParseColours(config, key, value);
                    break;
                case SkinIniCommon.FontsSection:
                    ParseFonts(config, key, value);
                    break;
            }
        }

        return config;
    }

    private static void ParseGeneral(SkinConfig config, string key, string value)
    {
        switch (SkinIniCommon.NormalizeKey(key))
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
            case "hitcircleoverlayabovenumber":
            case "hitcircleoverlayabovenumer":
                config.HitCircleOverlayAboveNumber = value == "1";
                break;
        }
    }

    private static void ParseColours(SkinConfig config, string key, string value)
    {
        var comboMatch = SkinIniCommon.ComboRegex().Match(key);
        if (comboMatch.Success)
        {
            var index = int.Parse(comboMatch.Groups[1].Value);
            if (SkinIniCommon.TryParseRgb(value, out var r, out var g, out var b))
            {
                config.ComboColours.Add(new ComboColour(index, r, g, b));
            }

            return;
        }

        switch (SkinIniCommon.NormalizeKey(key))
        {
            case "sliderborder":
                if (SkinIniCommon.TryParseRgb(value, out var sliderBorderR, out var sliderBorderG, out var sliderBorderB))
                {
                    config.SliderBorder = new RgbColour(sliderBorderR, sliderBorderG, sliderBorderB);
                }

                break;
            case "slidertrackoverride":
                if (SkinIniCommon.TryParseRgb(value, out var sliderTrackR, out var sliderTrackG, out var sliderTrackB))
                {
                    config.SliderTrackOverride = new RgbColour(sliderTrackR, sliderTrackG, sliderTrackB);
                }

                break;
        }
    }

    private static void ParseFonts(SkinConfig config, string key, string value)
    {
        switch (SkinIniCommon.NormalizeKey(key))
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
}
