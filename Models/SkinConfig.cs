namespace OsuInstaFadeSkinGenerator.Models;

public class SkinConfig
{
    public string Name { get; set; } = "Unknown";

    public string Author { get; set; } = string.Empty;

    public string Version { get; set; } = "1.0";

    public bool HitCircleOverlayAboveNumber { get; set; } = true;

    public List<ComboColour> ComboColours { get; set; } = new();

    public RgbColour? SliderBorder { get; set; }

    public RgbColour? SliderTrackOverride { get; set; }

    public string HitCirclePrefix { get; set; } = "default";

    public int HitCircleOverlap { get; set; } = -2;

    public string HitCirclePrefixDirectory
    {
        get
        {
            var lastSep = this.HitCirclePrefix.LastIndexOfAny(new[] { '/', '\\' });
            return lastSep >= 0 ? this.HitCirclePrefix[..lastSep] : string.Empty;
        }
    }
}
