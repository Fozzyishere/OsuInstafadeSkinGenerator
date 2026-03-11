namespace OsuInstaFadeSkinGenerator.Models;

public class ComboColour
{
    public ComboColour(int index, byte r, byte g, byte b)
    {
        this.Index = index;
        this.R = r;
        this.G = g;
        this.B = b;
    }

    public int Index { get; set; }

    public byte R { get; set; }

    public byte G { get; set; }

    public byte B { get; set; }
}
