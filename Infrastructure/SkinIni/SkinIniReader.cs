using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using OsuInstaFadeSkinGenerator.Application.Ports;
using OsuInstaFadeSkinGenerator.Domain;

namespace OsuInstaFadeSkinGenerator.Infrastructure.SkinIni;

public sealed class SkinIniReader : ISkinIniReader
{
    private readonly IFileSystem fileSystem;

    public SkinIniReader(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem;
    }

    public async Task<SkinConfig> ReadAsync(string skinIniPath, CancellationToken cancellationToken)
    {
        var builder = new SkinConfigBuilder();
        var lines = await this.fileSystem.ReadAllLinesAsync(skinIniPath, cancellationToken).ConfigureAwait(false);
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
                    ParseGeneral(builder, key, value);
                    break;
                case SkinIniCommon.ColoursSection:
                    ParseColours(builder, key, value);
                    break;
                case SkinIniCommon.FontsSection:
                    ParseFonts(builder, key, value);
                    break;
            }
        }

        return builder.Build();
    }

    private static void ParseGeneral(SkinConfigBuilder builder, string key, string value)
    {
        switch (SkinIniCommon.NormalizeKey(key))
        {
            case "name":
                builder.Name = value;
                break;
            case "author":
                builder.Author = value;
                break;
            case "version":
                builder.Version = value;
                break;
            case "hitcircleoverlayabovenumber":
            case "hitcircleoverlayabovenumer":
                if (SkinIniCommon.TryParseOsuBoolean(value, out var hitCircleOverlayAboveNumber))
                {
                    builder.HitCircleOverlayAboveNumber = hitCircleOverlayAboveNumber;
                }

                break;
        }
    }

    private static void ParseColours(SkinConfigBuilder builder, string key, string value)
    {
        var comboMatch = SkinIniCommon.ComboRegex().Match(key);
        if (comboMatch.Success)
        {
            var index = int.Parse(comboMatch.Groups[1].Value);
            if (RgbColor.TryParseCsv(value, out var color))
            {
                builder.ComboColours.Add((index, color));
            }

            return;
        }

        switch (SkinIniCommon.NormalizeKey(key))
        {
            case "sliderborder":
                if (RgbColor.TryParseCsv(value, out var sliderBorder))
                {
                    builder.SliderBorder = sliderBorder;
                }

                break;
            case "slidertrackoverride":
                if (RgbColor.TryParseCsv(value, out var sliderTrackOverride))
                {
                    builder.SliderTrackOverride = sliderTrackOverride;
                }

                break;
        }
    }

    private static void ParseFonts(SkinConfigBuilder builder, string key, string value)
    {
        switch (SkinIniCommon.NormalizeKey(key))
        {
            case "hitcircleprefix":
                if (!string.IsNullOrEmpty(value))
                {
                    builder.HitCirclePrefix = value;
                }

                break;
            case "hitcircleoverlap":
                if (int.TryParse(SkinIniCommon.TrimInlineComment(value), out var overlap))
                {
                    builder.HitCircleOverlap = overlap;
                }

                break;
        }
    }

    private sealed class SkinConfigBuilder
    {
        public string Name { get; set; } = "Unknown";

        public string Author { get; set; } = string.Empty;

        public string Version { get; set; } = "1.0";

        public bool HitCircleOverlayAboveNumber { get; set; } = true;

        public List<(int Index, RgbColor Color)> ComboColours { get; } = new();

        public RgbColor? SliderBorder { get; set; }

        public RgbColor? SliderTrackOverride { get; set; }

        public string HitCirclePrefix { get; set; } = "default";

        public int HitCircleOverlap { get; set; } = -2;

        public SkinConfig Build() => new(
            this.Name,
            this.Author,
            this.Version,
            this.HitCircleOverlayAboveNumber,
            new ReadOnlyCollection<(int Index, RgbColor Color)>(this.ComboColours.ToArray()),
            this.SliderBorder,
            this.SliderTrackOverride,
            this.HitCirclePrefix,
            this.HitCircleOverlap);
    }
}
