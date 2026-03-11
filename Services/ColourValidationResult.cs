using OsuInstaFadeSkinGenerator.Models;

namespace OsuInstaFadeSkinGenerator.Services;

public sealed record ColourValidationResult(ColourSelection? Colour, string? ErrorMessage)
{
    public bool IsValid => this.ErrorMessage == null;

    public bool HasValue => this.Colour != null;

    public static ColourValidationResult Empty => new(null, null);

    public static ColourValidationResult Invalid(string errorMessage) => new(null, errorMessage);

    public static ColourValidationResult Valid(ColourSelection colour) => new(colour, null);
}
