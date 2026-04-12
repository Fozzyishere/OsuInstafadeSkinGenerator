using OsuInstaFadeSkinGenerator.Models;
using OsuInstaFadeSkinGenerator.Services;
using SixLabors.ImageSharp.PixelFormats;

namespace OsuInstaFadeSkinGenerator.Tests;

public sealed class InstaFadeGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Template2_PerformsInstaFadeWorkflowAndUpdatesSkinIni()
    {
        using var skinDir = new TestSkinDirectory();
        var templateContent = SkinIniTemplateFixture.GetTemplateContent(2);
        var prefix = SkinIniTemplateFixture.ParseSupportedFields(templateContent).HitCirclePrefix;
        SkinIniTemplateFixture.WriteTemplateSkinIni(skinDir.RootPath, 2);
        CreateBaseAssets(skinDir.RootPath);
        SkinTestHelper.CreateNumberAssets(skinDir.RootPath, prefix);

        var generator = CreateGenerator();
        var result = await generator.GenerateAsync(
            CreateRequest(skinDir.RootPath, processHd: false, backupFiles: false, enableTripleStacking: false));

        Assert.True(result.Success, result.Message);

        var blankPath = SkinTestHelper.ResolvePrefixPath(skinDir.RootPath, prefix, "0");
        var numberPath = SkinTestHelper.ResolvePrefixPath(skinDir.RootPath, prefix, "1");
        Assert.True(File.Exists(blankPath));
        Assert.True(File.Exists(numberPath));

        using (var blank = SkinTestHelper.LoadPng(blankPath))
        {
            Assert.Equal(5, blank.Width);
            Assert.Equal(5, blank.Height);
            for (int y = 0; y < blank.Height; y++)
            {
                for (int x = 0; x < blank.Width; x++)
                {
                    Assert.Equal((byte)0, blank[x, y].A);
                }
            }
        }

        using (var number = SkinTestHelper.LoadPng(numberPath))
        {
            Assert.Equal(5, number.Width);
            Assert.Equal(5, number.Height);
        }

        SkinTestHelper.AssertFullyTransparent(Path.Combine(skinDir.RootPath, "hitcircle.png"));
        SkinTestHelper.AssertFullyTransparent(Path.Combine(skinDir.RootPath, "hitcircleoverlay.png"));

        var updatedSkinIni = File.ReadAllText(Path.Combine(skinDir.RootPath, "skin.ini"));
        SkinIniTemplateFixture.AssertUpdatedSkinIni(templateContent, updatedSkinIni, "10,20,30", 5);
    }

    [Fact]
    public async Task GenerateAsync_Template4_BacksUpOriginalAssetsGeneratedNumbersAndSkinIni()
    {
        using var skinDir = new TestSkinDirectory();
        var templateContent = SkinIniTemplateFixture.GetTemplateContent(4);
        var prefix = SkinIniTemplateFixture.ParseSupportedFields(templateContent).HitCirclePrefix;
        SkinIniTemplateFixture.WriteTemplateSkinIni(skinDir.RootPath, 4);
        CreateBaseAssets(skinDir.RootPath);
        SkinTestHelper.WriteFilledPng(Path.Combine(skinDir.RootPath, "hitcircle@2x.png"), 8, 8, new Rgba32(200, 0, 0, 255));
        SkinTestHelper.WriteFilledPng(Path.Combine(skinDir.RootPath, "hitcircleoverlay@2x.png"), 8, 8, new Rgba32(0, 200, 0, 255));
        SkinTestHelper.WriteFilledPng(SkinTestHelper.ResolvePrefixPath(skinDir.RootPath, prefix, "1"), 2, 2, new Rgba32(255, 255, 255, 255));
        SkinTestHelper.WriteFilledPng(SkinTestHelper.ResolvePrefixPath(skinDir.RootPath, prefix, "5", "@2x"), 4, 4, new Rgba32(255, 255, 255, 255));

        var generator = CreateGenerator();

        var result = await generator.GenerateAsync(
            CreateRequest(skinDir.RootPath, processHd: false, backupFiles: true, enableTripleStacking: false));

        Assert.True(result.Success, result.Message);

        var backupDir = Path.Combine(skinDir.RootPath, "_insta-fade-backup");
        Assert.True(File.Exists(Path.Combine(backupDir, "hitcircle.png")));
        Assert.True(File.Exists(Path.Combine(backupDir, "hitcircleoverlay.png")));
        Assert.True(File.Exists(Path.Combine(backupDir, "hitcircle@2x.png")));
        Assert.True(File.Exists(Path.Combine(backupDir, "hitcircleoverlay@2x.png")));
        Assert.True(File.Exists(SkinTestHelper.ResolvePrefixPath(backupDir, prefix, "1")));
        Assert.True(File.Exists(SkinTestHelper.ResolvePrefixPath(backupDir, prefix, "5", "@2x")));
        Assert.True(File.Exists(Path.Combine(backupDir, "skin.ini")));

        var backedUpSkinIni = File.ReadAllText(Path.Combine(backupDir, "skin.ini"));
        Assert.Equal(templateContent, backedUpSkinIni);
    }

    [Fact]
    public async Task GenerateAsync_Template1_SkipsHdWhenRequestedWithoutRequiredHdAssets()
    {
        using var skinDir = new TestSkinDirectory();
        var templateContent = SkinIniTemplateFixture.GetTemplateContent(1);
        var prefix = SkinIniTemplateFixture.ParseSupportedFields(templateContent).HitCirclePrefix;
        SkinIniTemplateFixture.WriteTemplateSkinIni(skinDir.RootPath, 1);
        CreateBaseAssets(skinDir.RootPath);
        SkinTestHelper.CreateNumberAssets(skinDir.RootPath, prefix);
        var progress = new RecordingProgress();
        var generator = CreateGenerator();

        var result = await generator.GenerateAsync(
            CreateRequest(skinDir.RootPath, processHd: true, backupFiles: false, enableTripleStacking: false),
            progress);

        Assert.True(result.Success, result.Message);
        Assert.Contains(progress.Entries, entry => entry.Message.Contains("Missing required HD asset", StringComparison.Ordinal));
        Assert.Contains(progress.Entries, entry => entry.Message.Contains("Skipping HD generation", StringComparison.Ordinal));
        Assert.False(File.Exists(SkinTestHelper.ResolvePrefixPath(skinDir.RootPath, prefix, "1", "@2x")));

        var updatedSkinIni = File.ReadAllText(Path.Combine(skinDir.RootPath, "skin.ini"));
        SkinIniTemplateFixture.AssertUpdatedSkinIni(templateContent, updatedSkinIni, "10,20,30", 5);
    }

    [Fact]
    public async Task GenerateAsync_Template3_TripleStackingRestoresMergedBaseAssetsAndKeepsMania()
    {
        using var skinDir = new TestSkinDirectory();
        var templateContent = SkinIniTemplateFixture.GetTemplateContent(3);
        var prefix = SkinIniTemplateFixture.ParseSupportedFields(templateContent).HitCirclePrefix;
        SkinIniTemplateFixture.WriteTemplateSkinIni(skinDir.RootPath, 3);
        CreateBaseAssets(skinDir.RootPath);
        SkinTestHelper.CreateNumberAssets(skinDir.RootPath, prefix);
        var generator = CreateGenerator();

        var result = await generator.GenerateAsync(
            CreateRequest(skinDir.RootPath, processHd: false, backupFiles: false, enableTripleStacking: true));

        Assert.True(result.Success, result.Message);

        using var hitcircle = SkinTestHelper.LoadPng(Path.Combine(skinDir.RootPath, "hitcircle.png"));
        using var overlay = SkinTestHelper.LoadPng(Path.Combine(skinDir.RootPath, "hitcircleoverlay.png"));

        Assert.Equal(4, hitcircle.Width);
        Assert.Equal(4, hitcircle.Height);
        Assert.Equal(4, overlay.Width);
        Assert.Equal(4, overlay.Height);
        Assert.Equal(hitcircle[0, 0], overlay[0, 0]);
        Assert.Equal(hitcircle[2, 2], overlay[2, 2]);
        Assert.Equal(new Rgba32(255, 0, 0, 255), hitcircle[0, 0]);
        Assert.Equal(new Rgba32(0, 255, 0, 255), hitcircle[2, 2]);

        var updatedSkinIni = File.ReadAllText(Path.Combine(skinDir.RootPath, "skin.ini"));
        SkinIniTemplateFixture.AssertUpdatedSkinIni(templateContent, updatedSkinIni, "10,20,30", 5);
    }

    private static InstaFadeGenerator CreateGenerator()
    {
        return new InstaFadeGenerator(new SkinIniReader(), new SkinIniWriter());
    }

    private static GenerationRequest CreateRequest(
        string skinFolder,
        bool processHd,
        bool backupFiles,
        bool enableTripleStacking)
    {
        return new GenerationRequest(
            skinFolder,
            10,
            20,
            30,
            processHd,
            backupFiles,
            enableTripleStacking);
    }

    private static void CreateBaseAssets(string skinFolder)
    {
        SkinTestHelper.WriteFilledPng(Path.Combine(skinFolder, "hitcircle.png"), 4, 4, new Rgba32(255, 0, 0, 255));
        SkinTestHelper.WritePng(
            Path.Combine(skinFolder, "hitcircleoverlay.png"),
            4,
            4,
            image =>
            {
                image[1, 1] = new Rgba32(0, 255, 0, 255);
                image[1, 2] = new Rgba32(0, 255, 0, 255);
                image[2, 1] = new Rgba32(0, 255, 0, 255);
                image[2, 2] = new Rgba32(0, 255, 0, 255);
            });
    }

    private sealed class RecordingProgress : IProgress<GenerationProgress>
    {
        public List<GenerationProgress> Entries { get; } = [];

        public void Report(GenerationProgress value)
        {
            this.Entries.Add(value);
        }
    }
}
