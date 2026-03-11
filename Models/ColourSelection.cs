namespace OsuInstaFadeSkinGenerator.Models;

public readonly record struct ColourSelection(byte R, byte G, byte B)
{
    public string Hex => $"#{this.R:X2}{this.G:X2}{this.B:X2}";
}
