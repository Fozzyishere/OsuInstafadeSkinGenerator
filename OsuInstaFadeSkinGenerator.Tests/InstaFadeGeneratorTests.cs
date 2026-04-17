using Microsoft.Extensions.Logging.Abstractions;
using OsuInstaFadeSkinGenerator.Application.Generation;
using OsuInstaFadeSkinGenerator.Infrastructure.Imaging;
using OsuInstaFadeSkinGenerator.Infrastructure.Io;
using OsuInstaFadeSkinGenerator.Infrastructure.SkinIni;
using OsuInstaFadeSkinGenerator.Domain;
using SixLabors.ImageSharp.PixelFormats;

namespace OsuInstaFadeSkinGenerator.Tests;

public sealed class InstaFadeGeneratorTests
{
    private static readonly Rgba32 HdHitcircleColor = new(200, 0, 0, 255);
    private static readonly Rgba32 HdOverlayColor = new(0, 200, 0, 255);
    private const int HdBaseAssetSize = 8;
    private const int HdNumberSize = 4;

    [Fact]
    public async Task GenerateAsync_Template2_PerformsInstaFadeWorkflowAndUpdatesSkinIni()
    {
        using var skinDir = new TestSkinDirectory();
        var fixture = new SkinFixtureBuilder(skinDir)
            .FromTemplate(2)
            .WithStandardBaseAssets()
            .WithStandardSdNumberAssets()
            .Build();

        var generator = CreateOrchestrator();
        var result = await generator.GenerateAsync(
            CreateRequest(fixture.RootPath, processHd: false, backupFiles: false, enableTripleStacking: false));

        Assert.Equal(GenerationStatus.Succeeded, result.Status);

        var blankPath = SkinTestHelper.ResolvePrefixPath(fixture.RootPath, fixture.Prefix, "0");
        var numberPath = SkinTestHelper.ResolvePrefixPath(fixture.RootPath, fixture.Prefix, "1");
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

        SkinTestHelper.AssertFullyTransparent(Path.Combine(fixture.RootPath, SkinAssetNames.Hitcircle));
        SkinTestHelper.AssertFullyTransparent(Path.Combine(fixture.RootPath, SkinAssetNames.HitcircleOverlay));

        var updatedSkinIni = File.ReadAllText(Path.Combine(fixture.RootPath, SkinAssetNames.SkinIni));
        SkinIniTemplateFixture.AssertUpdatedSkinIni(fixture.TemplateNumber, updatedSkinIni, "10,20,30", 5);
    }

    [Fact]
    public async Task GenerateAsync_Template4_BacksUpOriginalAssetsGeneratedNumbersAndSkinIni()
    {
        using var skinDir = new TestSkinDirectory();
        var templateContent = SkinIniTemplateFixture.GetTemplateContent(4);
        var fixture = new SkinFixtureBuilder(skinDir)
            .FromTemplate(4)
            .WithStandardBaseAssets()
            .WithHdBaseAssets(HdBaseAssetSize, HdHitcircleColor, HdOverlayColor)
            .WithSdNumber(1, SkinFixtureBuilder.DefaultSdNumberSize)
            .WithHdNumber(5, HdNumberSize)
            .Build();

        var generator = CreateOrchestrator();

        var result = await generator.GenerateAsync(
            CreateRequest(fixture.RootPath, processHd: false, backupFiles: true, enableTripleStacking: false));

        Assert.Equal(GenerationStatus.Succeeded, result.Status);

        var backupDir = Path.Combine(fixture.RootPath, SkinAssetNames.BackupFolder);
        Assert.True(File.Exists(Path.Combine(backupDir, SkinAssetNames.Hitcircle)));
        Assert.True(File.Exists(Path.Combine(backupDir, SkinAssetNames.HitcircleOverlay)));
        Assert.True(File.Exists(Path.Combine(backupDir, SkinAssetNames.WithHd(SkinAssetNames.Hitcircle))));
        Assert.True(File.Exists(Path.Combine(backupDir, SkinAssetNames.WithHd(SkinAssetNames.HitcircleOverlay))));
        Assert.True(File.Exists(SkinTestHelper.ResolvePrefixPath(backupDir, fixture.Prefix, "1")));
        Assert.True(File.Exists(SkinTestHelper.ResolvePrefixPath(backupDir, fixture.Prefix, "5", SkinAssetNames.HdSuffix)));
        Assert.True(File.Exists(Path.Combine(backupDir, SkinAssetNames.SkinIni)));

        var backedUpSkinIni = File.ReadAllText(Path.Combine(backupDir, SkinAssetNames.SkinIni));
        Assert.Equal(templateContent, backedUpSkinIni);
    }

    [Fact]
    public async Task GenerateAsync_Template1_SkipsHdWhenRequestedWithoutRequiredHdAssets()
    {
        using var skinDir = new TestSkinDirectory();
        var fixture = new SkinFixtureBuilder(skinDir)
            .FromTemplate(1)
            .WithStandardBaseAssets()
            .WithStandardSdNumberAssets()
            .Build();
        var progress = new RecordingProgress();
        var generator = CreateOrchestrator();

        var result = await generator.GenerateAsync(
            CreateRequest(fixture.RootPath, processHd: true, backupFiles: false, enableTripleStacking: false),
            progress);

        Assert.Equal(GenerationStatus.Succeeded, result.Status);
        Assert.Contains(progress.Entries, entry => entry.Phase == GenerationPhase.ProcessingHd && entry.Warning == GenerationError.MissingHdAsset);
        Assert.False(File.Exists(SkinTestHelper.ResolvePrefixPath(fixture.RootPath, fixture.Prefix, "1", SkinAssetNames.HdSuffix)));

        var updatedSkinIni = File.ReadAllText(Path.Combine(fixture.RootPath, SkinAssetNames.SkinIni));
        SkinIniTemplateFixture.AssertUpdatedSkinIni(fixture.TemplateNumber, updatedSkinIni, "10,20,30", 5);
    }

    [Fact]
    public async Task GenerateAsync_Template3_TripleStackingRestoresMergedBaseAssetsAndKeepsMania()
    {
        using var skinDir = new TestSkinDirectory();
        var fixture = new SkinFixtureBuilder(skinDir)
            .FromTemplate(3)
            .WithStandardBaseAssets()
            .WithStandardSdNumberAssets()
            .Build();
        var generator = CreateOrchestrator();

        var result = await generator.GenerateAsync(
            CreateRequest(fixture.RootPath, processHd: false, backupFiles: false, enableTripleStacking: true));

        Assert.Equal(GenerationStatus.Succeeded, result.Status);

        using var hitcircle = SkinTestHelper.LoadPng(Path.Combine(fixture.RootPath, SkinAssetNames.Hitcircle));
        using var overlay = SkinTestHelper.LoadPng(Path.Combine(fixture.RootPath, SkinAssetNames.HitcircleOverlay));

        Assert.Equal(SkinFixtureBuilder.DefaultBaseAssetSize, hitcircle.Width);
        Assert.Equal(SkinFixtureBuilder.DefaultBaseAssetSize, hitcircle.Height);
        Assert.Equal(SkinFixtureBuilder.DefaultBaseAssetSize, overlay.Width);
        Assert.Equal(SkinFixtureBuilder.DefaultBaseAssetSize, overlay.Height);
        Assert.Equal(hitcircle[0, 0], overlay[0, 0]);
        Assert.Equal(hitcircle[2, 2], overlay[2, 2]);
        Assert.Equal(SkinFixtureBuilder.DefaultHitcircleColor, hitcircle[0, 0]);
        Assert.Equal(SkinFixtureBuilder.DefaultOverlayColor, hitcircle[2, 2]);

        var updatedSkinIni = File.ReadAllText(Path.Combine(fixture.RootPath, SkinAssetNames.SkinIni));
        SkinIniTemplateFixture.AssertUpdatedSkinIni(fixture.TemplateNumber, updatedSkinIni, "10,20,30", 5);
    }

    private static IGenerationService CreateOrchestrator()
    {
        var fileSystem = new PhysicalFileSystem();
        return new InstaFadeGenerationOrchestrator(
            new SkinIniReader(fileSystem),
            new SkinIniWriter(fileSystem),
            fileSystem,
            new ImageSharpImageIo(),
            NullLogger<InstaFadeGenerationOrchestrator>.Instance);
    }

    private static GenerationRequest CreateRequest(
        string skinFolder,
        bool processHd,
        bool backupFiles,
        bool enableTripleStacking)
    {
        return new GenerationRequest(
            skinFolder,
            new RgbColor(10, 20, 30),
            processHd,
            backupFiles,
            enableTripleStacking);
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
