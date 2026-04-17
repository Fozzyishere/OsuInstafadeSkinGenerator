using System;
using System.IO;
using System.Threading.Tasks;
using OsuInstaFadeSkinGenerator.Domain;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace OsuInstaFadeSkinGenerator.Application.Generation;

internal static class ResilientFileOperations
{
    public static T Run<T>(Func<T> operation, GenerationError error, string action)
    {
        try
        {
            return operation();
        }
        catch (PathTooLongException ex)
        {
            throw new GenerationFailureException(error, $"Failed to {action}. The path is too long.", ex);
        }
        catch (IOException ex)
        {
            throw new GenerationFailureException(error, $"Failed to {action}. The file is in use or could not be accessed.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new GenerationFailureException(error, $"Failed to {action}. Access was denied.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new GenerationFailureException(error, $"Failed to {action}. The path is invalid.", ex);
        }
        catch (NotSupportedException ex)
        {
            throw new GenerationFailureException(error, $"Failed to {action}. The path is not supported.", ex);
        }
    }

    public static void Run(Action operation, GenerationError error, string action)
    {
        Run<int>(
            () =>
            {
                operation();
                return 0;
            },
            error,
            action);
    }

    public static async Task<T> RunAsync<T>(Func<Task<T>> operation, GenerationError error, string action)
    {
        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (PathTooLongException ex)
        {
            throw new GenerationFailureException(error, $"Failed to {action}. The path is too long.", ex);
        }
        catch (IOException ex)
        {
            throw new GenerationFailureException(error, $"Failed to {action}. The file is in use or could not be accessed.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new GenerationFailureException(error, $"Failed to {action}. Access was denied.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new GenerationFailureException(error, $"Failed to {action}. The path is invalid.", ex);
        }
        catch (NotSupportedException ex)
        {
            throw new GenerationFailureException(error, $"Failed to {action}. The path is not supported.", ex);
        }
    }

    public static Task RunAsync(Func<Task> operation, GenerationError error, string action) =>
        RunAsync(
            async () =>
            {
                await operation().ConfigureAwait(false);
                return 0;
            },
            error,
            action);

    public static async Task<Image<Rgba32>> RunImageLoadAsync(Func<Task<Image<Rgba32>>> operation, string displayPath)
    {
        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (PathTooLongException ex)
        {
            throw new GenerationFailureException(GenerationError.IoFailure, $"Failed to load {displayPath}. The path is too long.", ex);
        }
        catch (IOException ex)
        {
            throw new GenerationFailureException(GenerationError.IoFailure, $"Failed to load {displayPath}. The file is in use or unreadable.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new GenerationFailureException(GenerationError.IoFailure, $"Failed to load {displayPath}. Access was denied.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new GenerationFailureException(GenerationError.IoFailure, $"Failed to load {displayPath}. The file path is invalid.", ex);
        }
        catch (UnknownImageFormatException ex)
        {
            throw new GenerationFailureException(GenerationError.ImageDecodeFailure, $"Failed to load {displayPath}. The image format is not supported.", ex);
        }
        catch (InvalidImageContentException ex)
        {
            throw new GenerationFailureException(GenerationError.ImageDecodeFailure, $"Failed to load {displayPath}. The image data is invalid or corrupted.", ex);
        }
        catch (NotSupportedException ex)
        {
            throw new GenerationFailureException(GenerationError.ImageDecodeFailure, $"Failed to load {displayPath}. The image format is not supported.", ex);
        }
    }
}
