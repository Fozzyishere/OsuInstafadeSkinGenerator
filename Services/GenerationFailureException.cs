namespace OsuInstaFadeSkinGenerator.Services;

public sealed class GenerationFailureException : Exception
{
    public GenerationFailureException(string message)
        : base(message)
    {
    }

    public GenerationFailureException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
