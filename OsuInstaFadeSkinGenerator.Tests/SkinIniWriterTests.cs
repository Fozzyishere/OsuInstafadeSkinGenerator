using OsuInstaFadeSkinGenerator.Services;

namespace OsuInstaFadeSkinGenerator.Tests;

public sealed class SkinIniWriterTests
{
    [Theory]
    [InlineData(2, 7, 8, 9, 64)]
    [InlineData(3, 4, 5, 6, 77)]
    public void Update_Template_RewritesSupportedFieldsAndPreservesOtherLines(
        int templateNumber,
        byte comboR,
        byte comboG,
        byte comboB,
        int hitCircleOverlap)
    {
        using var skinDir = new TestSkinDirectory();
        var originalContent = SkinIniTemplateFixture.GetTemplateContent(templateNumber);
        SkinIniTemplateFixture.WriteTemplateSkinIni(skinDir.RootPath, templateNumber);

        var writer = new SkinIniWriter();
        var skinIniPath = Path.Combine(skinDir.RootPath, "skin.ini");

        writer.Update(skinIniPath, comboR, comboG, comboB, hitCircleOverlap);

        var updated = File.ReadAllText(skinIniPath);
        SkinIniTemplateFixture.AssertUpdatedSkinIni(
            originalContent,
            updated,
            $"{comboR},{comboG},{comboB}",
            hitCircleOverlap);
    }

    [Fact]
    public void Update_TemplateDerivedSparseIni_AppendsMissingSectionsAndUsesDetectedIndent()
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

        var writer = new SkinIniWriter();
        var skinIniPath = Path.Combine(skinDir.RootPath, "skin.ini");

        writer.Update(skinIniPath, 1, 2, 3, 55);

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
