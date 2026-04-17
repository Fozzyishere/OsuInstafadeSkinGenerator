using System;
using OsuInstaFadeSkinGenerator.Models;

namespace OsuInstaFadeSkinGenerator.Services;

public sealed class GenerationFailureException : Exception
{
    public GenerationFailureException(GenerationError error, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        this.Error = error;
    }

    public GenerationError Error { get; }
}
