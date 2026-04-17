using OsuInstaFadeSkinGenerator.Application.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace OsuInstaFadeSkinGenerator.Tests;

public sealed class ImageProcessorTests
{
    [Theory]
    [InlineData(4, 4, 1.25f, 5, 5)]
    [InlineData(8, 8, 1.25f, 10, 10)]
    [InlineData(16, 8, 1.25f, 20, 10)]
    [InlineData(4, 4, 2.0f, 8, 8)]
    public void Upscale_ProducesExpectedDimensions(int w, int h, float factor, int expectedW, int expectedH)
    {
        using var source = new Image<Rgba32>(w, h, new Rgba32(255, 255, 255, 255));

        using var result = ImageProcessor.Upscale(source, factor);

        Assert.Equal(expectedW, result.Width);
        Assert.Equal(expectedH, result.Height);
    }

    [Fact]
    public void Upscale_PreservesSourceImage()
    {
        using var source = new Image<Rgba32>(4, 4, new Rgba32(255, 255, 255, 255));

        using var result = ImageProcessor.Upscale(source, 1.25f);

        Assert.Equal(4, source.Width);
        Assert.Equal(4, source.Height);
    }

    [Fact]
    public void Tint_FullWhitePixel_BecomesTintColor()
    {
        using var image = new Image<Rgba32>(2, 2, new Rgba32(255, 255, 255, 200));

        ImageProcessor.Tint(image, 10, 20, 30);

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                Assert.Equal(10, image[x, y].R);
                Assert.Equal(20, image[x, y].G);
                Assert.Equal(30, image[x, y].B);
                Assert.Equal(200, image[x, y].A);
            }
        }
    }

    [Fact]
    public void Tint_FullBlackPixel_StaysBlack()
    {
        using var image = new Image<Rgba32>(1, 1, new Rgba32(0, 0, 0, 255));

        ImageProcessor.Tint(image, 255, 128, 64);

        Assert.Equal(new Rgba32(0, 0, 0, 255), image[0, 0]);
    }

    [Fact]
    public void Tint_HalfIntensity_ScalesLinearly()
    {
        using var image = new Image<Rgba32>(1, 1, new Rgba32(200, 100, 50, 255));

        ImageProcessor.Tint(image, 255, 255, 255);

        Assert.Equal(200, image[0, 0].R);
        Assert.Equal(100, image[0, 0].G);
        Assert.Equal(50, image[0, 0].B);
    }

    [Theory]
    [InlineData(4, 4, 8, 8, 8, 8)]
    [InlineData(4, 8, 6, 4, 6, 8)]
    [InlineData(10, 2, 2, 10, 10, 10)]
    public void Composite_UsesMaxDimensions(int bw, int bh, int ow, int oh, int expectedW, int expectedH)
    {
        using var baseImage = new Image<Rgba32>(bw, bh, new Rgba32(255, 0, 0, 255));
        using var overlay = new Image<Rgba32>(ow, oh, new Rgba32(0, 255, 0, 255));

        using var result = ImageProcessor.Composite(baseImage, overlay);

        Assert.Equal(expectedW, result.Width);
        Assert.Equal(expectedH, result.Height);
    }

    [Fact]
    public void CreateBlank_AllPixelsAreFullyTransparent()
    {
        using var image = ImageProcessor.CreateBlank(6, 4);

        Assert.Equal(6, image.Width);
        Assert.Equal(4, image.Height);
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                Assert.Equal(0, image[x, y].A);
            }
        }
    }

    [Theory]
    [InlineData(4, 4, 6, 6, 8, 8, 8, 8)]
    [InlineData(10, 4, 6, 8, 4, 2, 10, 8)]
    [InlineData(2, 2, 2, 2, 2, 2, 2, 2)]
    public void ComposeNumberBetween_UsesMaxOfAllThreeDimensions(
        int hw, int hh, int nw, int nh, int ow, int oh, int expectedW, int expectedH)
    {
        using var hitcircle = new Image<Rgba32>(hw, hh, new Rgba32(255, 0, 0, 255));
        using var number = new Image<Rgba32>(nw, nh, new Rgba32(255, 255, 255, 255));
        using var overlay = new Image<Rgba32>(ow, oh, new Rgba32(0, 255, 0, 255));

        using var result = ImageProcessor.ComposeNumberBetween(hitcircle, number, overlay);

        Assert.Equal(expectedW, result.Width);
        Assert.Equal(expectedH, result.Height);
    }

    [Fact]
    public void PlaceNumberOnCircle_PreservesCircleDimensions()
    {
        using var circle = new Image<Rgba32>(8, 8, new Rgba32(255, 0, 0, 255));
        using var number = new Image<Rgba32>(2, 2, new Rgba32(255, 255, 255, 255));

        using var result = ImageProcessor.PlaceNumberOnCircle(circle, number);

        Assert.Equal(8, result.Width);
        Assert.Equal(8, result.Height);
    }

    [Fact]
    public void PlaceNumberOnCircle_PaintsNumberAtCenter()
    {
        using var circle = new Image<Rgba32>(4, 4, new Rgba32(0, 0, 0, 0));
        using var number = new Image<Rgba32>(2, 2, new Rgba32(255, 255, 255, 255));

        using var result = ImageProcessor.PlaceNumberOnCircle(circle, number);

        Assert.Equal(255, result[1, 1].A);
        Assert.Equal(255, result[2, 2].A);
        Assert.Equal(0, result[0, 0].A);
        Assert.Equal(0, result[3, 3].A);
    }

    [Fact]
    public void PlaceNumberOnCircle_DoesNotMutateOriginalCircle()
    {
        using var circle = new Image<Rgba32>(4, 4, new Rgba32(0, 0, 0, 0));
        using var number = new Image<Rgba32>(2, 2, new Rgba32(255, 255, 255, 255));

        _ = ImageProcessor.PlaceNumberOnCircle(circle, number);

        Assert.Equal(0, circle[1, 1].A);
        Assert.Equal(0, circle[2, 2].A);
    }
}
