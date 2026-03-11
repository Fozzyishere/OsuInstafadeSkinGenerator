namespace OsuInstaFadeSkinGenerator.Services;

public static class GenerationCoordinator
{
    public static Task<InstaFadeGenerator.GenerationResult> GenerateAsync(
        InstaFadeGenerator.GenerationOptions options,
        Action<double, string>? progress = null)
    {
        return Task.Run(() => InstaFadeGenerator.Generate(options, progress));
    }
}
