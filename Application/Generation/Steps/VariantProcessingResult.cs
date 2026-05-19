using OsuInstaFadeSkinGenerator.Domain;

namespace OsuInstaFadeSkinGenerator.Application.Generation.Steps;

internal readonly record struct VariantProcessingResult(
    GenerationOutcome Outcome,
    int GeneratedDefaultNumberWidth);
