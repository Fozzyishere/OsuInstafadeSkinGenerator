namespace OsuInstaFadeSkinGenerator.Application.Generation;

internal readonly record struct ProgressRange(double Start, double End)
{
    public double Span => this.End - this.Start;

    public double Interpolate(double fraction) => this.Start + (this.Span * fraction);
}
