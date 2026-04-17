using System.Collections.Generic;

namespace OsuInstaFadeSkinGenerator.Models;

public sealed record SkinConfig(
    string Name,
    string Author,
    string Version,
    bool HitCircleOverlayAboveNumber,
    IReadOnlyList<(int Index, RgbColor Color)> ComboColours,
    RgbColor? SliderBorder,
    RgbColor? SliderTrackOverride,
    string HitCirclePrefix,
    int HitCircleOverlap)
{
    private static readonly char[] PrefixSeparators = ['/', '\\'];

    public string HitCirclePrefixDirectory { get; } = ComputePrefixDir(HitCirclePrefix);

    private static string ComputePrefixDir(string prefix)
    {
        var lastSep = prefix.LastIndexOfAny(PrefixSeparators);
        return lastSep >= 0 ? prefix[..lastSep] : string.Empty;
    }
}
