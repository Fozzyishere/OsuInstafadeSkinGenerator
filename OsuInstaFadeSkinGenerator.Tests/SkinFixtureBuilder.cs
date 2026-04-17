using OsuInstaFadeSkinGenerator.Domain;
using SixLabors.ImageSharp.PixelFormats;

namespace OsuInstaFadeSkinGenerator.Tests;

internal sealed class SkinFixtureBuilder
{
    internal static readonly Rgba32 DefaultHitcircleColor = new(255, 0, 0, 255);
    internal static readonly Rgba32 DefaultOverlayColor = new(0, 255, 0, 255);
    internal static readonly Rgba32 DefaultNumberColor = new(255, 255, 255, 255);
    internal const int DefaultBaseAssetSize = 4;
    internal const int DefaultSdNumberSize = 2;

    private readonly TestSkinDirectory skinDirectory;
    private int? templateNumber;
    private bool standardBaseAssets;
    private HdBaseAssetSpec? hdBaseAssets;
    private bool standardSdNumberAssets;
    private readonly List<NumberAssetSpec> numberAssets = [];

    public SkinFixtureBuilder(TestSkinDirectory skinDirectory)
    {
        this.skinDirectory = skinDirectory;
    }

    public SkinFixtureBuilder FromTemplate(int templateNumber)
    {
        this.templateNumber = templateNumber;
        return this;
    }

    public SkinFixtureBuilder WithStandardBaseAssets()
    {
        this.standardBaseAssets = true;
        return this;
    }

    public SkinFixtureBuilder WithHdBaseAssets(int size, Rgba32 hitcircleColor, Rgba32 overlayColor)
    {
        this.hdBaseAssets = new HdBaseAssetSpec(size, hitcircleColor, overlayColor);
        return this;
    }

    public SkinFixtureBuilder WithStandardSdNumberAssets()
    {
        this.standardSdNumberAssets = true;
        return this;
    }

    public SkinFixtureBuilder WithSdNumber(int number, int size, Rgba32? color = null)
    {
        this.numberAssets.Add(new NumberAssetSpec(number, size, color ?? DefaultNumberColor, string.Empty));
        return this;
    }

    public SkinFixtureBuilder WithHdNumber(int number, int size, Rgba32? color = null)
    {
        this.numberAssets.Add(new NumberAssetSpec(number, size, color ?? DefaultNumberColor, SkinAssetNames.HdSuffix));
        return this;
    }

    public SkinFixture Build()
    {
        if (this.templateNumber is null)
        {
            throw new InvalidOperationException("Template number must be set before building the fixture.");
        }

        SkinIniTemplateFixture.WriteTemplateSkinIni(this.skinDirectory.RootPath, this.templateNumber.Value);
        var prefix = SkinIniTemplateFixture.GetExpected(this.templateNumber.Value).HitCirclePrefix;

        if (this.standardBaseAssets)
        {
            WriteStandardBaseAssets(this.skinDirectory.RootPath);
        }

        if (this.hdBaseAssets is { } hd)
        {
            SkinTestHelper.WriteFilledPng(
                Path.Combine(this.skinDirectory.RootPath, SkinAssetNames.WithHd(SkinAssetNames.Hitcircle)),
                hd.Size,
                hd.Size,
                hd.HitcircleColor);
            SkinTestHelper.WriteFilledPng(
                Path.Combine(this.skinDirectory.RootPath, SkinAssetNames.WithHd(SkinAssetNames.HitcircleOverlay)),
                hd.Size,
                hd.Size,
                hd.OverlayColor);
        }

        if (this.standardSdNumberAssets)
        {
            SkinTestHelper.CreateNumberAssets(this.skinDirectory.RootPath, prefix);
        }

        foreach (var number in this.numberAssets)
        {
            SkinTestHelper.WriteFilledPng(
                SkinTestHelper.ResolvePrefixPath(this.skinDirectory.RootPath, prefix, number.Number.ToString(), number.Suffix),
                number.Size,
                number.Size,
                number.Color);
        }

        return new SkinFixture(this.skinDirectory.RootPath, prefix, this.templateNumber.Value);
    }

    private static void WriteStandardBaseAssets(string skinFolder)
    {
        SkinTestHelper.WriteFilledPng(
            Path.Combine(skinFolder, SkinAssetNames.Hitcircle),
            DefaultBaseAssetSize,
            DefaultBaseAssetSize,
            DefaultHitcircleColor);

        SkinTestHelper.WritePng(
            Path.Combine(skinFolder, SkinAssetNames.HitcircleOverlay),
            DefaultBaseAssetSize,
            DefaultBaseAssetSize,
            image =>
            {
                image[1, 1] = DefaultOverlayColor;
                image[1, 2] = DefaultOverlayColor;
                image[2, 1] = DefaultOverlayColor;
                image[2, 2] = DefaultOverlayColor;
            });
    }

    private readonly record struct HdBaseAssetSpec(int Size, Rgba32 HitcircleColor, Rgba32 OverlayColor);

    private readonly record struct NumberAssetSpec(int Number, int Size, Rgba32 Color, string Suffix);
}

internal sealed record SkinFixture(string RootPath, string Prefix, int TemplateNumber);
