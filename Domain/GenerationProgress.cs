namespace OsuInstaFadeSkinGenerator.Domain;

public readonly record struct GenerationProgress(
    GenerationPhase Phase,
    double Fraction,
    string Message,
    GenerationError? Warning = null);
