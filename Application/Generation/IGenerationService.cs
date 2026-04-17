using System;
using System.Threading;
using System.Threading.Tasks;
using OsuInstaFadeSkinGenerator.Models;

namespace OsuInstaFadeSkinGenerator.Application.Generation;

public interface IGenerationService
{
    Task<GenerationOutcome> GenerateAsync(
        GenerationRequest request,
        IProgress<GenerationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
