using OsuInstaFadeSkinGenerator.Domain;
using OsuInstaFadeSkinGenerator.Infrastructure.Io;
using OsuInstaFadeSkinGenerator.Infrastructure.SkinIni;

namespace OsuInstaFadeSkinGenerator.Tests;

public sealed class SkinIniWriterTests
{
    [Theory]
    [InlineData(1, 9, 8, 7, 22)]
    [InlineData(2, 7, 8, 9, 64)]
    [InlineData(3, 4, 5, 6, 77)]
    [InlineData(4, 250, 240, 230, 12)]
    public async Task Update_Template_RewritesSupportedFieldsAndPreservesOtherLines(
        int templateNumber,
        byte comboR,
        byte comboG,
        byte comboB,
        int hitCircleOverlap)
    {
        using var skinDir = new TestSkinDirectory();
        SkinIniTemplateFixture.WriteTemplateSkinIni(skinDir.RootPath, templateNumber);

        var writer = new SkinIniWriter(new PhysicalFileSystem());
        var skinIniPath = Path.Combine(skinDir.RootPath, SkinAssetNames.SkinIni);

        await writer.UpdateAsync(skinIniPath, new RgbColor(comboR, comboG, comboB), hitCircleOverlap, CancellationToken.None);

        var updated = File.ReadAllText(skinIniPath);
        SkinIniTemplateFixture.AssertUpdatedSkinIni(
            templateNumber,
            updated,
            $"{comboR},{comboG},{comboB}",
            hitCircleOverlap);
    }

    [Fact]
    public async Task Update_TemplateDerivedSparseIni_AppendsMissingSectionsAndUsesDetectedIndent()
    {
        using var skinDir = new TestSkinDirectory();
        SkinTestHelper.WriteSkinIni(
            skinDir.RootPath,
            """
            //Formatted by ck // pepega tools // cyperdark#6890
            [General]
                Name: - Example Skin
                Author: cyperdark
                ||=====
                || Downloaded from https://ck1t.ru/ss
                ||=====
                Version: latest
            """);

        var writer = new SkinIniWriter(new PhysicalFileSystem());
        var skinIniPath = Path.Combine(skinDir.RootPath, SkinAssetNames.SkinIni);

        await writer.UpdateAsync(skinIniPath, new RgbColor(1, 2, 3), 55, CancellationToken.None);

        var updated = File.ReadAllText(skinIniPath);

        Assert.Contains("[Colours]", updated);
        Assert.Contains("    Combo1: 1,2,3", updated);
        Assert.Contains("[Fonts]", updated);
        Assert.Contains("    HitCircleOverlap: 55", updated);
        Assert.Contains("    Name: - Example Skin", updated);
        Assert.Contains("    Author: cyperdark", updated);
        Assert.Contains("    || Downloaded from https://ck1t.ru/ss", updated);
    }
}
