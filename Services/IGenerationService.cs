using System.Threading;
using System.Threading.Tasks;
using OsuInstaFadeSkinGenerator.Models;

namespace OsuInstaFadeSkinGenerator.Services;

public interface IGenerationService
{
    Task<GenerationOutcome> GenerateAsync(
        GenerationRequest request,
        IProgress<GenerationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
