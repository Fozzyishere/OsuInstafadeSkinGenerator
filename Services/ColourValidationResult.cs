using OsuInstaFadeSkinGenerator.Models;

namespace OsuInstaFadeSkinGenerator.Services;

public sealed record ColourValidationResult(RgbColor? Colour, string? ErrorMessage)
{
    public bool IsValid => this.ErrorMessage == null;

    public bool HasValue => this.Colour != null;

    public static ColourValidationResult Empty => new(null, null);

    public static ColourValidationResult Invalid(string errorMessage) => new(null, errorMessage);

    public static ColourValidationResult Valid(RgbColor colour) => new(colour, null);
}
