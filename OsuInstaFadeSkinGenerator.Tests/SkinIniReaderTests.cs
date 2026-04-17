using OsuInstaFadeSkinGenerator.Services;

namespace OsuInstaFadeSkinGenerator.Tests;

public sealed class SkinIniReaderTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Read_Template_ParsesSupportedFields(int templateNumber)
    {
        using var skinDir = new TestSkinDirectory();
        var templateContent = SkinIniTemplateFixture.GetTemplateContent(templateNumber);
        SkinIniTemplateFixture.WriteTemplateSkinIni(skinDir.RootPath, templateNumber);

        var reader = new SkinIniReader();
        var config = reader.Read(Path.Combine(skinDir.RootPath, "skin.ini"));

        SkinIniTemplateFixture.AssertSupportedFieldsMatch(templateContent, config);
    }

    [Fact]
    public void Read_TemplateDerivedMixedCasingAndInvalidValues_LeavesDefaults()
    {
        using var skinDir = new TestSkinDirectory();
        var templateContent = SkinIniTemplateFixture.GetTemplateContent(4);
        var expected = SkinIniTemplateFixture.ParseSupportedFields(templateContent);
        var content = templateContent
            .Replace("[General]", "[gEnErAl]", StringComparison.Ordinal)
            .Replace("[Colours]", "[cOlOuRs]", StringComparison.Ordinal)
            .Replace("[Fonts]", "[fOnTs]", StringComparison.Ordinal)
            .Replace("HitCircleOverlayAboveNumber: 0", "HitCircleOverlayAboveNumer: maybe", StringComparison.Ordinal)
            .Replace("Combo1: 241,214,207 // #f1d6cf", "combo1: nope", StringComparison.Ordinal)
            .Replace("HitCirclePrefix: default", "HitCirclePrefix:", StringComparison.Ordinal)
            .Replace("HitCircleOverlap: 25", "HitCircleOverlap: invalid", StringComparison.Ordinal);
        SkinTestHelper.WriteSkinIni(skinDir.RootPath, content);

        var reader = new SkinIniReader();
        var config = reader.Read(Path.Combine(skinDir.RootPath, "skin.ini"));

        Assert.Equal(expected.Name, config.Name);
        Assert.Equal(expected.Author, config.Author);
        Assert.Equal(expected.Version, config.Version);
        Assert.True(config.HitCircleOverlayAboveNumber);
        Assert.DoesNotContain(config.ComboColours, combo => combo.Index == 1);

        var expectedCombo2 = expected.ComboColours[2];
        var combo2 = Assert.Single(config.ComboColours, combo => combo.Index == 2);
        Assert.Equal(expectedCombo2.R, combo2.Color.R);
        Assert.Equal(expectedCombo2.G, combo2.Color.G);
        Assert.Equal(expectedCombo2.B, combo2.Color.B);

        Assert.Equal(expected.HitCirclePrefix, config.HitCirclePrefix);
        Assert.Equal(-2, config.HitCircleOverlap);
    }
}
