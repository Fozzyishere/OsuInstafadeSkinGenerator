using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OsuInstaFadeSkinGenerator.Application.Generation;
using OsuInstaFadeSkinGenerator.Application.Ports;
using OsuInstaFadeSkinGenerator.Domain;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace OsuInstaFadeSkinGenerator.Infrastructure.Imaging;

public sealed class ImageSharpImageIo : IImageIo
{
    private readonly IFileSystem fileSystem;

    public ImageSharpImageIo(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem;
    }

    public Task<Image<Rgba32>> LoadAsync(string path, CancellationToken cancellationToken)
    {
        var displayPath = GetDisplayPath(path);
        return ResilientFileOperations.RunImageLoadAsync(
            () => Image.LoadAsync<Rgba32>(path, cancellationToken),
            displayPath);
    }

    public Task SaveAsPngAsync(Image<Rgba32> image, string path, CancellationToken cancellationToken)
    {
        var displayPath = GetDisplayPath(path);
        return ResilientFileOperations.RunAsync(
            () => this.fileSystem.ReplaceFileAtomicallyAsync(
                path,
                (tempPath, ct) => image.SaveAsPngAsync(tempPath, ct),
                cancellationToken),
            GenerationError.IoFailure,
            $"write {displayPath}");
    }

    private static string GetDisplayPath(string path)
    {
        return Path.GetFileName(path) is { Length: > 0 } fileName ? fileName : path;
    }
}
