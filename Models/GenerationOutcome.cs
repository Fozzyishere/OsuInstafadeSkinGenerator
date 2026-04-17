namespace OsuInstaFadeSkinGenerator.Models;

public readonly record struct GenerationOutcome(
    GenerationStatus Status,
    GenerationError? Error,
    string? DetailMessage);
