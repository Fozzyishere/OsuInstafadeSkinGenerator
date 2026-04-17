using OsuInstaFadeSkinGenerator.Infrastructure.Io;
using OsuInstaFadeSkinGenerator.Infrastructure.SkinIni;
using OsuInstaFadeSkinGenerator.Domain;

namespace OsuInstaFadeSkinGenerator.Tests;

public sealed class SkinIniReaderTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task Read_Template_ParsesSupportedFields(int templateNumber)
    {
        using var skinDir = new TestSkinDirectory();
        SkinIniTemplateFixture.WriteTemplateSkinIni(skinDir.RootPath, templateNumber);

        var reader = new SkinIniReader(new PhysicalFileSystem());
        var config = await reader.ReadAsync(Path.Combine(skinDir.RootPath, SkinAssetNames.SkinIni), CancellationToken.None);

        SkinIniTemplateFixture.AssertSupportedFieldsMatch(templateNumber, config);
    }

    [Fact]
    public async Task Read_TemplateDerivedMixedCasingAndInvalidValues_LeavesDefaults()
    {
        using var skinDir = new TestSkinDirectory();
        var expected = SkinIniTemplateFixture.GetExpected(4);
        var templateContent = SkinIniTemplateFixture.GetTemplateContent(4);
        var content = templateContent
            .Replace("[General]", "[gEnErAl]", StringComparison.Ordinal)
            .Replace("[Colours]", "[cOlOuRs]", StringComparison.Ordinal)
            .Replace("[Fonts]", "[fOnTs]", StringComparison.Ordinal)
            .Replace("HitCircleOverlayAboveNumber: 0", "HitCircleOverlayAboveNumer: maybe", StringComparison.Ordinal)
            .Replace("Combo1: 241,214,207 // #f1d6cf", "combo1: nope", StringComparison.Ordinal)
            .Replace("HitCirclePrefix: default", "HitCirclePrefix:", StringComparison.Ordinal)
            .Replace("HitCircleOverlap: 25", "HitCircleOverlap: invalid", StringComparison.Ordinal);
        SkinTestHelper.WriteSkinIni(skinDir.RootPath, content);

        var reader = new SkinIniReader(new PhysicalFileSystem());
        var config = await reader.ReadAsync(Path.Combine(skinDir.RootPath, SkinAssetNames.SkinIni), CancellationToken.None);

        Assert.Equal(expected.Name, config.Name);
        Assert.Equal(expected.Author, config.Author);
        Assert.Equal(expected.Version, config.Version);
        Assert.True(config.HitCircleOverlayAboveNumber);
        Assert.DoesNotContain(config.ComboColours, combo => combo.Index == 1);

        var expectedCombo2 = expected.ComboColours.Single(combo => combo.Index == 2).Color;
        var combo2 = Assert.Single(config.ComboColours, combo => combo.Index == 2);
        Assert.Equal(expectedCombo2, combo2.Color);

        Assert.Equal(expected.HitCirclePrefix, config.HitCirclePrefix);
        Assert.Equal(-2, config.HitCircleOverlap);
    }
}
