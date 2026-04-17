using OsuInstaFadeSkinGenerator.Domain;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace OsuInstaFadeSkinGenerator.Tests;

internal static class SkinTestHelper
{
    public static void WriteSkinIni(string skinFolder, string content)
    {
        File.WriteAllText(
            Path.Combine(skinFolder, SkinAssetNames.SkinIni),
            content.ReplaceLineEndings(Environment.NewLine));
    }

    public static void CreateNumberAssets(string skinFolder, string prefix, string suffix = "")
    {
        for (int i = 1; i <= 9; i++)
        {
            WriteFilledPng(ResolvePrefixPath(skinFolder, prefix, i.ToString(), suffix), 2, 2, new Rgba32(255, 255, 255, 255));
        }
    }

    public static void WriteFilledPng(string path, int width, int height, Rgba32 colour)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var image = new Image<Rgba32>(width, height, colour);
        image.SaveAsPng(path);
    }

    public static void WritePng(string path, int width, int height, Action<Image<Rgba32>> draw)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var image = new Image<Rgba32>(width, height, new Rgba32(0, 0, 0, 0));
        draw(image);
        image.SaveAsPng(path);
    }

    public static Image<Rgba32> LoadPng(string path)
    {
        return Image.Load<Rgba32>(path);
    }

    public static string ResolvePrefixPath(string skinFolder, string prefix, string numberSuffix, string hdSuffix = "")
    {
        var normalizedPrefix = prefix
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        return Path.Combine(skinFolder, $"{normalizedPrefix}-{numberSuffix}{hdSuffix}.png");
    }

    public static void AssertFullyTransparent(string path)
    {
        using var image = LoadPng(path);
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                Assert.Equal((byte)0, image[x, y].A);
            }
        }
    }
}
