using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace OsuInstaFadeSkinGenerator.Services;

public static class ImageProcessor
{
    public static Image<Rgba32> Upscale(Image<Rgba32> source, float factor)
    {
        int newWidth = (int)Math.Round(source.Width * factor);
        int newHeight = (int)Math.Round(source.Height * factor);
        var result = source.Clone();
        result.Mutate(x => x.Resize(newWidth, newHeight, KnownResamplers.Lanczos3));
        return result;
    }

    public static void Tint(Image<Rgba32> image, byte r, byte g, byte b)
    {
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    ref var pixel = ref row[x];
                    pixel.R = (byte)(pixel.R * r / 255);
                    pixel.G = (byte)(pixel.G * g / 255);
                    pixel.B = (byte)(pixel.B * b / 255);
                }
            }
        });
    }

    public static Image<Rgba32> Composite(Image<Rgba32> baseImage, Image<Rgba32> overlay)
    {
        int width = Math.Max(baseImage.Width, overlay.Width);
        int height = Math.Max(baseImage.Height, overlay.Height);

        var result = new Image<Rgba32>(width, height, new Rgba32(0, 0, 0, 0));

        result.Mutate(x =>
        {
            DrawCentered(x, baseImage, width, height);
            DrawCentered(x, overlay, width, height);
        });

        return result;
    }

    public static Image<Rgba32> PlaceNumberOnCircle(Image<Rgba32> mergedCircle, Image<Rgba32> numberImage)
    {
        var result = mergedCircle.Clone();
        result.Mutate(x => x.DrawImage(numberImage, GetCenteredPoint(result.Width, result.Height, numberImage), 1f));
        return result;
    }

    public static Image<Rgba32> ComposeNumberBetween(
        Image<Rgba32> hitcircle, Image<Rgba32> numberImage, Image<Rgba32> overlay)
    {
        int width = Math.Max(Math.Max(hitcircle.Width, overlay.Width), numberImage.Width);
        int height = Math.Max(Math.Max(hitcircle.Height, overlay.Height), numberImage.Height);

        var result = new Image<Rgba32>(width, height, new Rgba32(0, 0, 0, 0));

        result.Mutate(x =>
        {
            DrawCentered(x, hitcircle, width, height);
            DrawCentered(x, numberImage, width, height);
            DrawCentered(x, overlay, width, height);
        });

        return result;
    }

    public static Image<Rgba32> CreateBlank(int width, int height)
    {
        return new Image<Rgba32>(width, height, new Rgba32(0, 0, 0, 0));
    }

    private static void DrawCentered(IImageProcessingContext context, Image<Rgba32> image, int canvasWidth, int canvasHeight)
    {
        context.DrawImage(image, GetCenteredPoint(canvasWidth, canvasHeight, image), 1f);
    }

    private static Point GetCenteredPoint(int canvasWidth, int canvasHeight, Image<Rgba32> image)
    {
        return new Point(
            (canvasWidth - image.Width) / 2,
            (canvasHeight - image.Height) / 2);
    }
}
