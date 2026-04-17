namespace OsuInstaFadeSkinGenerator.Domain;

public readonly record struct GenerationOutcome(
    GenerationStatus Status,
    GenerationError? Error,
    string? DetailMessage);
