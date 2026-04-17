namespace OsuInstaFadeSkinGenerator.Domain;

public abstract record ColourValidation
{
    private ColourValidation()
    {
    }

    public sealed record Empty : ColourValidation;

    public sealed record Invalid(string Message) : ColourValidation;

    public sealed record Valid(RgbColor Color) : ColourValidation;
}
