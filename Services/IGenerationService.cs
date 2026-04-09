using OsuInstaFadeSkinGenerator.Models;

namespace OsuInstaFadeSkinGenerator.Services;

public interface IGenerationService
{
    Task<GenerationResult> GenerateAsync(
        GenerationRequest request,
        IProgress<GenerationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
