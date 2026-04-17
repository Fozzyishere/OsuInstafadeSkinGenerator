using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace OsuInstaFadeSkinGenerator.Application.Ports;

public interface IImageIo
{
    Task<Image<Rgba32>> LoadAsync(string path, CancellationToken cancellationToken);

    Task SaveAsPngAsync(Image<Rgba32> image, string path, CancellationToken cancellationToken);
}
