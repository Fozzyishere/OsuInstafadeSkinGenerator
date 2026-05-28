using Microsoft.Extensions.Logging.Abstractions;
using OsuInstaFadeSkinGenerator.Application.Generation;
using OsuInstaFadeSkinGenerator.Application.Ports;
using OsuInstaFadeSkinGenerator.Domain;
using OsuInstaFadeSkinGenerator.Infrastructure.Imaging;
using OsuInstaFadeSkinGenerator.Infrastructure.Io;
using OsuInstaFadeSkinGenerator.Infrastructure.SkinIni;
using SixLabors.ImageSharp.PixelFormats;

namespace OsuInstaFadeSkinGenerator.Tests;

public sealed class InstaFadeGenerationOrchestratorTests
{
    private static readonly Rgba32 HdHitcircleColor = new(200, 0, 0, 255);
    private static readonly Rgba32 HdOverlayColor = new(0, 200, 0, 255);
    private const int HdBaseAssetSize = 8;

    [Fact]
    public async Task GenerateAsync_HappyPath_ReturnsSucceededWithDoneProgress()
    {
        using var skinDir = new TestSkinDirectory();
        var fixture = new SkinFixtureBuilder(skinDir)
            .FromTemplate(2)
            .WithStandardBaseAssets()
            .WithStandardSdNumberAssets()
            .Build();
        var progress = new RecordingProgress();
        var orchestrator = CreateOrchestrator();

        var result = await orchestrator.GenerateAsync(
            CreateRequest(fixture.RootPath, processHd: false, backupFiles: false, enableTripleStacking: false),
            progress);

        Assert.Equal(GenerationStatus.Succeeded, result.Status);
        Assert.Null(result.Error);
        Assert.Contains(progress.Entries, entry => entry.Phase == GenerationPhase.Done);
    }

    [Fact]
    public async Task GenerateAsync_CustomNestedHitCirclePrefix_Succeeds()
    {
        using var skinDir = new TestSkinDirectory();
        var fixture = new SkinFixtureBuilder(skinDir)
            .FromTemplate(2)
            .WithStandardBaseAssets()
            .Build();
        var hitCirclePrefix = Path.Combine("numbers", "default");
        SkinTestHelper.WriteSkinIni(
            fixture.RootPath,
            SkinIniTemplateFixture.GetTemplateContentWithHitCirclePrefix(fixture.TemplateNumber, hitCirclePrefix));
        SkinTestHelper.CreateNumberAssets(fixture.RootPath, hitCirclePrefix);

        var orchestrator = CreateOrchestrator();

        var result = await orchestrator.GenerateAsync(
            CreateRequest(fixture.RootPath, processHd: false, backupFiles: false, enableTripleStacking: false));

        Assert.Equal(GenerationStatus.Succeeded, result.Status);
        Assert.Null(result.Error);
        Assert.True(File.Exists(SkinTestHelper.ResolvePrefixPath(fixture.RootPath, hitCirclePrefix, "1")));
        Assert.True(File.Exists(SkinTestHelper.ResolvePrefixPath(fixture.RootPath, hitCirclePrefix, "0")));
        Assert.Contains(
            $"HitCirclePrefix: {hitCirclePrefix}",
            File.ReadAllText(Path.Combine(fixture.RootPath, SkinAssetNames.SkinIni)));
    }

    [Theory]
    [MemberData(nameof(UnsafeHitCirclePrefixes))]
    public async Task GenerateAsync_UnsafeHitCirclePrefix_ReturnsFailedAndLeavesSkinUntouched(string hitCirclePrefix)
    {
        using var skinDir = new TestSkinDirectory();
        var fixture = new SkinFixtureBuilder(skinDir)
            .FromTemplate(2)
            .WithStandardBaseAssets()
            .Build();
        SkinTestHelper.WriteSkinIni(
            fixture.RootPath,
            SkinIniTemplateFixture.GetTemplateContentWithHitCirclePrefix(fixture.TemplateNumber, hitCirclePrefix));

        var orchestrator = CreateOrchestrator();

        var result = await orchestrator.GenerateAsync(
            CreateRequest(fixture.RootPath, processHd: false, backupFiles: false, enableTripleStacking: false));

        Assert.Equal(GenerationStatus.Failed, result.Status);
        Assert.Equal(GenerationError.UnsafeOutputPath, result.Error);
        Assert.Contains("HitCirclePrefix", result.DetailMessage ?? string.Empty);
        Assert.Equal(
            SkinIniTemplateFixture.GetTemplateContentWithHitCirclePrefix(fixture.TemplateNumber, hitCirclePrefix),
            File.ReadAllText(Path.Combine(fixture.RootPath, SkinAssetNames.SkinIni)));
        using var hitcircle = SkinTestHelper.LoadPng(Path.Combine(fixture.RootPath, SkinAssetNames.Hitcircle));
        using var overlay = SkinTestHelper.LoadPng(Path.Combine(fixture.RootPath, SkinAssetNames.HitcircleOverlay));
        Assert.Equal(SkinFixtureBuilder.DefaultBaseAssetSize, hitcircle.Width);
        Assert.Equal(SkinFixtureBuilder.DefaultBaseAssetSize, hitcircle.Height);
        Assert.Equal(SkinFixtureBuilder.DefaultHitcircleColor, hitcircle[0, 0]);
        Assert.Equal(SkinFixtureBuilder.DefaultBaseAssetSize, overlay.Width);
        Assert.Equal(SkinFixtureBuilder.DefaultBaseAssetSize, overlay.Height);
        Assert.Equal(SkinFixtureBuilder.DefaultOverlayColor, overlay[1, 1]);
    }

    [Fact]
    public async Task GenerateAsync_SkinFolderMissing_ReturnsFailedWithSkinFolderMissingError()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var orchestrator = CreateOrchestrator();

        var result = await orchestrator.GenerateAsync(CreateRequest(nonExistentPath));

        Assert.Equal(GenerationStatus.Failed, result.Status);
        Assert.Equal(GenerationError.SkinFolderMissing, result.Error);
    }

    [Fact]
    public async Task GenerateAsync_SkinIniMissing_ReturnsFailedWithSkinIniMissingError()
    {
        using var skinDir = new TestSkinDirectory();
        var orchestrator = CreateOrchestrator();

        var result = await orchestrator.GenerateAsync(CreateRequest(skinDir.RootPath));

        Assert.Equal(GenerationStatus.Failed, result.Status);
        Assert.Equal(GenerationError.SkinIniMissing, result.Error);
    }

    [Fact]
    public async Task GenerateAsync_MissingHdAsset_SucceedsButEmitsHdWarning()
    {
        using var skinDir = new TestSkinDirectory();
        var fixture = new SkinFixtureBuilder(skinDir)
            .FromTemplate(1)
            .WithStandardBaseAssets()
            .WithStandardSdNumberAssets()
            .Build();
        var progress = new RecordingProgress();
        var orchestrator = CreateOrchestrator();

        var result = await orchestrator.GenerateAsync(
            CreateRequest(fixture.RootPath, processHd: true),
            progress);

        Assert.Equal(GenerationStatus.Succeeded, result.Status);
        Assert.Contains(
            progress.Entries,
            entry => entry.Phase == GenerationPhase.ProcessingHd && entry.Warning == GenerationError.MissingHdAsset);
    }

    [Fact]
    public async Task GenerateAsync_CorruptPngAsHitcircle_ReturnsFailedWithImageDecodeFailure()
    {
        using var skinDir = new TestSkinDirectory();
        var fixture = new SkinFixtureBuilder(skinDir)
            .FromTemplate(2)
            .WithStandardSdNumberAssets()
            .Build();
        File.WriteAllText(Path.Combine(fixture.RootPath, SkinAssetNames.Hitcircle), "not a png");
        File.WriteAllText(Path.Combine(fixture.RootPath, SkinAssetNames.HitcircleOverlay), "not a png");

        var orchestrator = CreateOrchestrator();

        var result = await orchestrator.GenerateAsync(CreateRequest(fixture.RootPath));

        Assert.Equal(GenerationStatus.Failed, result.Status);
        Assert.Equal(GenerationError.ImageDecodeFailure, result.Error);
    }

    [Fact]
    public async Task GenerateAsync_IoFailureReadingSkinIni_ReturnsFailedWithIoFailure()
    {
        using var skinDir = new TestSkinDirectory();
        SkinIniTemplateFixture.WriteTemplateSkinIni(skinDir.RootPath, 1);
        var fileSystem = new ThrowingFileSystem(
            new PhysicalFileSystem(),
            onReadAllLinesAsync: _ => throw new IOException("file in use"));
        var orchestrator = CreateOrchestratorWith(fileSystem);

        var result = await orchestrator.GenerateAsync(CreateRequest(skinDir.RootPath));

        Assert.Equal(GenerationStatus.Failed, result.Status);
        Assert.Equal(GenerationError.IoFailure, result.Error);
    }

    [Fact]
    public async Task GenerateAsync_UnexpectedException_ReturnsFailedWithUnexpected()
    {
        using var skinDir = new TestSkinDirectory();
        SkinIniTemplateFixture.WriteTemplateSkinIni(skinDir.RootPath, 1);
        var fileSystem = new ThrowingFileSystem(
            new PhysicalFileSystem(),
            onDirectoryExists: _ => throw new OutOfMemoryException("simulated unexpected"));
        var orchestrator = CreateOrchestratorWith(fileSystem);

        var result = await orchestrator.GenerateAsync(CreateRequest(skinDir.RootPath));

        Assert.Equal(GenerationStatus.Failed, result.Status);
        Assert.Equal(GenerationError.Unexpected, result.Error);
    }

    [Fact]
    public async Task GenerateAsync_CancelledBeforeStart_ReturnsCancelled()
    {
        using var skinDir = new TestSkinDirectory();
        SkinIniTemplateFixture.WriteTemplateSkinIni(skinDir.RootPath, 1);
        var orchestrator = CreateOrchestrator();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await orchestrator.GenerateAsync(CreateRequest(skinDir.RootPath), cancellationToken: cts.Token);

        Assert.Equal(GenerationStatus.Cancelled, result.Status);
    }

    [Fact]
    public async Task GenerateAsync_CancelledDuringStaging_LeavesSkinUnchanged()
    {
        using var skinDir = new TestSkinDirectory();
        var fixture = new SkinFixtureBuilder(skinDir)
            .FromTemplate(2)
            .WithStandardBaseAssets()
            .WithStandardSdNumberAssets()
            .Build();

        using var cts = new CancellationTokenSource();
        var stagedWriteCount = 0;
        var fileSystem = new ThrowingFileSystem(
            new PhysicalFileSystem(),
            onReplaceFileAtomicallyAsync: (destinationPath, _, _) =>
            {
                if (!destinationPath.Contains(".insta-fade-work", StringComparison.OrdinalIgnoreCase)
                    || Path.GetExtension(destinationPath) != ".png")
                {
                    return;
                }

                stagedWriteCount++;
                if (stagedWriteCount == 3)
                {
                    cts.Cancel();
                }
            });

        var orchestrator = CreateOrchestratorWith(fileSystem);

        var result = await orchestrator.GenerateAsync(CreateRequest(fixture.RootPath), cancellationToken: cts.Token);

        Assert.Equal(GenerationStatus.Cancelled, result.Status);
        Assert.Equal(
            SkinIniTemplateFixture.GetTemplateContent(fixture.TemplateNumber),
            File.ReadAllText(Path.Combine(fixture.RootPath, SkinAssetNames.SkinIni)));
        AssertOriginalSdSkinPreserved(fixture);
    }

    [Fact]
    public async Task GenerateAsync_CancelledDuringBackupStaging_LeavesSkinAndExistingBackupUnchanged()
    {
        using var skinDir = new TestSkinDirectory();
        var fixture = new SkinFixtureBuilder(skinDir)
            .FromTemplate(2)
            .WithStandardBaseAssets()
            .WithStandardSdNumberAssets()
            .Build();
        var backupDir = Path.Combine(fixture.RootPath, SkinAssetNames.BackupFolder);
        Directory.CreateDirectory(backupDir);
        var previousBackupPath = Path.Combine(backupDir, SkinAssetNames.SkinIni);
        const string previousBackup = "previous-backup";
        File.WriteAllText(previousBackupPath, previousBackup);

        using var cts = new CancellationTokenSource();
        var stagedBackupCount = 0;
        var fileSystem = new ThrowingFileSystem(
            new PhysicalFileSystem(),
            onCopyFileAtomicallyAsync: (_, destinationPath, _) =>
            {
                if (!destinationPath.Contains(".insta-fade-work", StringComparison.OrdinalIgnoreCase)
                    || !Path.GetExtension(destinationPath).Equals(".png", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                stagedBackupCount++;
                if (stagedBackupCount == 2)
                {
                    cts.Cancel();
                }
            });

        var orchestrator = CreateOrchestratorWith(fileSystem);

        var result = await orchestrator.GenerateAsync(
            CreateRequest(fixture.RootPath, backupFiles: true),
            cancellationToken: cts.Token);

        Assert.Equal(GenerationStatus.Cancelled, result.Status);
        Assert.Equal(previousBackup, File.ReadAllText(previousBackupPath));
        AssertOriginalSdSkinPreserved(fixture);
    }

    [Fact]
    public async Task GenerateAsync_CancelledDuringSkinIniStaging_LeavesSkinUnchanged()
    {
        using var skinDir = new TestSkinDirectory();
        var fixture = new SkinFixtureBuilder(skinDir)
            .FromTemplate(2)
            .WithStandardBaseAssets()
            .WithStandardSdNumberAssets()
            .Build();

        using var cts = new CancellationTokenSource();
        var fileSystem = new ThrowingFileSystem(
            new PhysicalFileSystem(),
            onCopyFileAtomicallyAsync: (_, destinationPath, _) =>
            {
                if (destinationPath.Contains(".insta-fade-work", StringComparison.OrdinalIgnoreCase)
                    && Path.GetExtension(destinationPath).Equals(".ini", StringComparison.OrdinalIgnoreCase))
                {
                    cts.Cancel();
                }
            });

        var orchestrator = CreateOrchestratorWith(fileSystem);

        var result = await orchestrator.GenerateAsync(CreateRequest(fixture.RootPath), cancellationToken: cts.Token);

        Assert.Equal(GenerationStatus.Cancelled, result.Status);
        AssertOriginalSdSkinPreserved(fixture);
    }

    [Fact]
    public async Task GenerateAsync_CancelledDuringSnapshot_LeavesSkinUnchanged()
    {
        using var skinDir = new TestSkinDirectory();
        var fixture = new SkinFixtureBuilder(skinDir)
            .FromTemplate(2)
            .WithStandardBaseAssets()
            .WithStandardSdNumberAssets()
            .Build();

        using var cts = new CancellationTokenSource();
        var snapshotCount = 0;
        var fileSystem = new ThrowingFileSystem(
            new PhysicalFileSystem(),
            onCopyFileAtomicallyAsync: (_, destinationPath, _) =>
            {
                if (!destinationPath.Contains("snapshot", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                snapshotCount++;
                if (snapshotCount == 2)
                {
                    cts.Cancel();
                }
            });

        var orchestrator = CreateOrchestratorWith(fileSystem);

        var result = await orchestrator.GenerateAsync(CreateRequest(fixture.RootPath), cancellationToken: cts.Token);

        Assert.Equal(GenerationStatus.Cancelled, result.Status);
        AssertOriginalSdSkinPreserved(fixture);
    }

    [Fact]
    public async Task GenerateAsync_CancelledDuringCommit_RollsBackCommittedFiles()
    {
        using var skinDir = new TestSkinDirectory();
        var fixture = new SkinFixtureBuilder(skinDir)
            .FromTemplate(2)
            .WithStandardBaseAssets()
            .WithStandardSdNumberAssets()
            .Build();

        using var cts = new CancellationTokenSource();
        var finalCommitCount = 0;
        var progress = new RecordingProgress();
        var fileSystem = new ThrowingFileSystem(
            new PhysicalFileSystem(),
            onCopyFileAtomicallyAsync: (_, destinationPath, cancellationToken) =>
            {
                if (destinationPath.Contains(".insta-fade-work", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                finalCommitCount++;
                if (finalCommitCount == 2)
                {
                    cts.Cancel();
                }
            });

        var orchestrator = CreateOrchestratorWith(fileSystem);

        var result = await orchestrator.GenerateAsync(CreateRequest(fixture.RootPath), progress, cts.Token);

        Assert.Equal(GenerationStatus.Cancelled, result.Status);
        Assert.Contains(progress.Entries, entry => entry.Phase == GenerationPhase.RollingBack);
        AssertOriginalSdSkinPreserved(fixture);
    }

    [Fact]
    public async Task GenerateAsync_CancelledDuringBackupCommit_RestoresPreviousBackup()
    {
        using var skinDir = new TestSkinDirectory();
        var fixture = new SkinFixtureBuilder(skinDir)
            .FromTemplate(4)
            .WithStandardBaseAssets()
            .WithHdBaseAssets(HdBaseAssetSize, HdHitcircleColor, HdOverlayColor)
            .WithStandardSdNumberAssets()
            .Build();
        var backupDir = Path.Combine(fixture.RootPath, SkinAssetNames.BackupFolder);
        Directory.CreateDirectory(backupDir);
        var previousBackupPath = Path.Combine(backupDir, SkinAssetNames.Hitcircle);
        const string previousBackup = "previous-good-backup";
        File.WriteAllText(previousBackupPath, previousBackup);

        using var cts = new CancellationTokenSource();
        var finalCommitCount = 0;
        var fileSystem = new ThrowingFileSystem(
            new PhysicalFileSystem(),
            onCopyFileAtomicallyAsync: (_, destinationPath, cancellationToken) =>
            {
                if (destinationPath.Contains(".insta-fade-work", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                finalCommitCount++;
                if (finalCommitCount == 2)
                {
                    cts.Cancel();
                }
            });

        var orchestrator = CreateOrchestratorWith(fileSystem);

        var result = await orchestrator.GenerateAsync(
            CreateRequest(fixture.RootPath, processHd: true, backupFiles: true),
            cancellationToken: cts.Token);

        Assert.Equal(GenerationStatus.Cancelled, result.Status);
        Assert.Equal(
            SkinIniTemplateFixture.GetTemplateContent(fixture.TemplateNumber),
            File.ReadAllText(Path.Combine(fixture.RootPath, SkinAssetNames.SkinIni)));
        Assert.Equal(previousBackup, File.ReadAllText(previousBackupPath));
        AssertOriginalSdSkinPreserved(fixture);
        AssertOriginalHdBaseAssetsPreserved(fixture);
    }

    [Fact]
    public async Task GenerateAsync_CancelledDuringBackupCommit_WithNestedPrefix_RestoresPreviousBackupTree()
    {
        using var skinDir = new TestSkinDirectory();
        var fixture = new SkinFixtureBuilder(skinDir)
            .FromTemplate(4)
            .WithStandardBaseAssets()
            .WithStandardSdNumberAssets()
            .Build();
        var hitCirclePrefix = Path.Combine("numbers", "default");
        SkinTestHelper.WriteSkinIni(
            fixture.RootPath,
            SkinIniTemplateFixture.GetTemplateContentWithHitCirclePrefix(fixture.TemplateNumber, hitCirclePrefix));
        SkinTestHelper.CreateNumberAssets(fixture.RootPath, hitCirclePrefix);

        var backupDir = Path.Combine(fixture.RootPath, SkinAssetNames.BackupFolder);
        var previousBackupDir = Path.Combine(backupDir, "previous-run", "numbers");
        Directory.CreateDirectory(previousBackupDir);
        var previousBackupPath = Path.Combine(previousBackupDir, "default-1.png");
        const string previousBackup = "previous-good-backup";
        File.WriteAllText(previousBackupPath, previousBackup);

        using var cts = new CancellationTokenSource();
        var finalCommitCount = 0;
        var fileSystem = new ThrowingFileSystem(
            new PhysicalFileSystem(),
            onCopyFileAtomicallyAsync: (_, destinationPath, cancellationToken) =>
            {
                if (destinationPath.Contains(".insta-fade-work", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                finalCommitCount++;
                if (finalCommitCount == 3)
                {
                    cts.Cancel();
                }
            });

        var orchestrator = CreateOrchestratorWith(fileSystem);

        var result = await orchestrator.GenerateAsync(
            CreateRequest(fixture.RootPath, processHd: false, backupFiles: true),
            cancellationToken: cts.Token);

        Assert.Equal(GenerationStatus.Cancelled, result.Status);
        Assert.Equal(previousBackup, File.ReadAllText(previousBackupPath));
        Assert.Equal(Path.Combine(backupDir, "previous-run"), Assert.Single(Directory.GetDirectories(backupDir)));

        Assert.Equal(
            SkinIniTemplateFixture.GetTemplateContentWithHitCirclePrefix(fixture.TemplateNumber, hitCirclePrefix),
            File.ReadAllText(Path.Combine(fixture.RootPath, SkinAssetNames.SkinIni)));

        using (var restoredNumber = SkinTestHelper.LoadPng(
                   SkinTestHelper.ResolvePrefixPath(fixture.RootPath, hitCirclePrefix, "1")))
        {
            Assert.Equal(SkinFixtureBuilder.DefaultSdNumberSize, restoredNumber.Width);
            Assert.Equal(SkinFixtureBuilder.DefaultSdNumberSize, restoredNumber.Height);
            Assert.Equal(SkinFixtureBuilder.DefaultNumberColor, restoredNumber[0, 0]);
        }

        using (var hitcircle = SkinTestHelper.LoadPng(Path.Combine(fixture.RootPath, SkinAssetNames.Hitcircle)))
        {
            Assert.Equal(SkinFixtureBuilder.DefaultBaseAssetSize, hitcircle.Width);
            Assert.Equal(SkinFixtureBuilder.DefaultBaseAssetSize, hitcircle.Height);
            Assert.Equal(SkinFixtureBuilder.DefaultHitcircleColor, hitcircle[0, 0]);
        }

        using (var overlay = SkinTestHelper.LoadPng(Path.Combine(fixture.RootPath, SkinAssetNames.HitcircleOverlay)))
        {
            Assert.Equal(SkinFixtureBuilder.DefaultBaseAssetSize, overlay.Width);
            Assert.Equal(SkinFixtureBuilder.DefaultBaseAssetSize, overlay.Height);
            Assert.Equal(SkinFixtureBuilder.DefaultOverlayColor, overlay[1, 1]);
        }
    }

    [Fact]
    public async Task GenerateAsync_FailureDuringCommit_RollsBackAndReturnsFailed()
    {
        using var skinDir = new TestSkinDirectory();
        var fixture = new SkinFixtureBuilder(skinDir)
            .FromTemplate(2)
            .WithStandardBaseAssets()
            .WithStandardSdNumberAssets()
            .Build();

        var finalCommitCount = 0;
        var progress = new RecordingProgress();
        var fileSystem = new ThrowingFileSystem(
            new PhysicalFileSystem(),
            onCopyFileAtomicallyAsync: (_, destinationPath, cancellationToken) =>
            {
                if (destinationPath.Contains(".insta-fade-work", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                finalCommitCount++;
                if (finalCommitCount == 2)
                {
                    throw new IOException("simulated commit failure");
                }
            });

        var orchestrator = CreateOrchestratorWith(fileSystem);

        var result = await orchestrator.GenerateAsync(CreateRequest(fixture.RootPath), progress);

        Assert.Equal(GenerationStatus.Failed, result.Status);
        Assert.Equal(GenerationError.IoFailure, result.Error);
        Assert.Contains(progress.Entries, entry => entry.Phase == GenerationPhase.RollingBack);
        AssertOriginalSdSkinPreserved(fixture);
    }

    private static InstaFadeGenerationOrchestrator CreateOrchestrator()
        => CreateOrchestratorWith(new PhysicalFileSystem());

    private static InstaFadeGenerationOrchestrator CreateOrchestratorWith(IFileSystem fileSystem)
    {
        return new InstaFadeGenerationOrchestrator(
            new SkinIniReader(fileSystem),
            new SkinIniWriter(fileSystem),
            fileSystem,
            new ImageSharpImageIo(fileSystem),
            NullLogger<InstaFadeGenerationOrchestrator>.Instance);
    }

    private static GenerationRequest CreateRequest(
        string skinFolder,
        bool processHd = false,
        bool backupFiles = false,
        bool enableTripleStacking = false)
    {
        return new GenerationRequest(
            skinFolder,
            new RgbColor(10, 20, 30),
            processHd,
            backupFiles,
            enableTripleStacking);
    }

    public static IEnumerable<object[]> UnsafeHitCirclePrefixes()
    {
        yield return ["../../escape"];
        yield return ["..\\..//escape"];
        yield return [Path.GetFullPath(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")))];
    }

    private static void AssertVariantCompleted(
        string skinFolder,
        string prefix,
        string variantSuffix,
        int expectedOutputSize,
        int expectedBaseSize)
    {
        for (var i = 1; i <= 9; i++)
        {
            AssertImageSize(SkinTestHelper.ResolvePrefixPath(skinFolder, prefix, i.ToString(), variantSuffix), expectedOutputSize);
        }

        var blankPath = SkinTestHelper.ResolvePrefixPath(skinFolder, prefix, "0", variantSuffix);
        AssertImageSize(blankPath, expectedOutputSize);
        SkinTestHelper.AssertFullyTransparent(blankPath);

        var hitcirclePath = GetBaseAssetPath(skinFolder, SkinAssetNames.Hitcircle, variantSuffix);
        AssertImageSize(hitcirclePath, expectedBaseSize);
        SkinTestHelper.AssertFullyTransparent(hitcirclePath);

        var overlayPath = GetBaseAssetPath(skinFolder, SkinAssetNames.HitcircleOverlay, variantSuffix);
        AssertImageSize(overlayPath, expectedBaseSize);
        SkinTestHelper.AssertFullyTransparent(overlayPath);
    }

    private static void AssertOriginalSdSkinPreserved(SkinFixture fixture)
    {
        Assert.Equal(
            SkinIniTemplateFixture.GetTemplateContent(fixture.TemplateNumber),
            File.ReadAllText(Path.Combine(fixture.RootPath, SkinAssetNames.SkinIni)));

        Assert.False(File.Exists(SkinTestHelper.ResolvePrefixPath(fixture.RootPath, fixture.Prefix, "0")));
        for (var i = 1; i <= 9; i++)
        {
            AssertImageSize(
                SkinTestHelper.ResolvePrefixPath(fixture.RootPath, fixture.Prefix, i.ToString()),
                SkinFixtureBuilder.DefaultSdNumberSize);
        }

        using (var hitcircle = SkinTestHelper.LoadPng(Path.Combine(fixture.RootPath, SkinAssetNames.Hitcircle)))
        {
            Assert.Equal(SkinFixtureBuilder.DefaultBaseAssetSize, hitcircle.Width);
            Assert.Equal(SkinFixtureBuilder.DefaultBaseAssetSize, hitcircle.Height);
            Assert.Equal(SkinFixtureBuilder.DefaultHitcircleColor, hitcircle[0, 0]);
        }

        using (var overlay = SkinTestHelper.LoadPng(Path.Combine(fixture.RootPath, SkinAssetNames.HitcircleOverlay)))
        {
            Assert.Equal(SkinFixtureBuilder.DefaultBaseAssetSize, overlay.Width);
            Assert.Equal(SkinFixtureBuilder.DefaultBaseAssetSize, overlay.Height);
            Assert.Equal(SkinFixtureBuilder.DefaultOverlayColor, overlay[1, 1]);
        }

        Assert.Empty(Directory.GetDirectories(fixture.RootPath, ".insta-fade-work-*"));
    }

    private static void AssertOriginalHdBaseAssetsPreserved(SkinFixture fixture)
    {
        var hdHitcirclePath = Path.Combine(fixture.RootPath, SkinAssetNames.WithHd(SkinAssetNames.Hitcircle));
        using (var hitcircle = SkinTestHelper.LoadPng(hdHitcirclePath))
        {
            Assert.Equal(HdBaseAssetSize, hitcircle.Width);
            Assert.Equal(HdBaseAssetSize, hitcircle.Height);
            Assert.Equal(HdHitcircleColor, hitcircle[0, 0]);
        }

        var hdOverlayPath = Path.Combine(fixture.RootPath, SkinAssetNames.WithHd(SkinAssetNames.HitcircleOverlay));
        using (var overlay = SkinTestHelper.LoadPng(hdOverlayPath))
        {
            Assert.Equal(HdBaseAssetSize, overlay.Width);
            Assert.Equal(HdBaseAssetSize, overlay.Height);
            Assert.Equal(HdOverlayColor, overlay[0, 0]);
        }

        for (var i = 0; i <= 9; i++)
        {
            Assert.False(File.Exists(SkinTestHelper.ResolvePrefixPath(
                fixture.RootPath,
                fixture.Prefix,
                i.ToString(),
                SkinAssetNames.HdSuffix)));
        }
    }

    private static string GetBaseAssetPath(string skinFolder, string baseFileName, string variantSuffix)
    {
        return string.Equals(variantSuffix, SkinAssetNames.HdSuffix, StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(skinFolder, SkinAssetNames.WithHd(baseFileName))
            : Path.Combine(skinFolder, baseFileName);
    }

    private static void AssertImageSize(string path, int expectedSize)
    {
        Assert.True(File.Exists(path));

        using var image = SkinTestHelper.LoadPng(path);
        Assert.Equal(expectedSize, image.Width);
        Assert.Equal(expectedSize, image.Height);
    }

    private sealed class RecordingProgress : IProgress<GenerationProgress>
    {
        public List<GenerationProgress> Entries { get; } = [];

        public void Report(GenerationProgress value) => this.Entries.Add(value);
    }

    private sealed class ThrowingFileSystem : IFileSystem
    {
        private readonly IFileSystem inner;
        private readonly Action<string>? onDirectoryExists;
        private readonly Action<string>? onFileExists;
        private readonly Action<string>? onReadAllLinesAsync;
        private readonly Action<string, IEnumerable<string>, CancellationToken>? onWriteAllLinesAtomicallyAsync;
        private readonly Action<string, Func<string, CancellationToken, Task>, CancellationToken>? onReplaceFileAtomicallyAsync;
        private readonly Action<string, string, CancellationToken>? onCopyFileAtomicallyAsync;

        public ThrowingFileSystem(
            IFileSystem inner,
            Action<string>? onDirectoryExists = null,
            Action<string>? onFileExists = null,
            Action<string>? onReadAllLinesAsync = null,
            Action<string, IEnumerable<string>, CancellationToken>? onWriteAllLinesAtomicallyAsync = null,
            Action<string, Func<string, CancellationToken, Task>, CancellationToken>? onReplaceFileAtomicallyAsync = null,
            Action<string, string, CancellationToken>? onCopyFileAtomicallyAsync = null)
        {
            this.inner = inner;
            this.onDirectoryExists = onDirectoryExists;
            this.onFileExists = onFileExists;
            this.onReadAllLinesAsync = onReadAllLinesAsync;
            this.onWriteAllLinesAtomicallyAsync = onWriteAllLinesAtomicallyAsync;
            this.onReplaceFileAtomicallyAsync = onReplaceFileAtomicallyAsync;
            this.onCopyFileAtomicallyAsync = onCopyFileAtomicallyAsync;
        }

        public bool DirectoryExists(string path)
        {
            this.onDirectoryExists?.Invoke(path);
            return this.inner.DirectoryExists(path);
        }

        public bool FileExists(string path)
        {
            this.onFileExists?.Invoke(path);
            return this.inner.FileExists(path);
        }

        public void CreateDirectory(string path) => this.inner.CreateDirectory(path);

        public string CreateTemporaryDirectory(string parentDirectory, string prefix)
            => this.inner.CreateTemporaryDirectory(parentDirectory, prefix);

        public void CopyFile(string sourcePath, string destinationPath, bool overwrite)
            => this.inner.CopyFile(sourcePath, destinationPath, overwrite);

        public Task CopyFileAtomicallyAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
        {
            this.onCopyFileAtomicallyAsync?.Invoke(sourcePath, destinationPath, cancellationToken);
            return this.inner.CopyFileAtomicallyAsync(sourcePath, destinationPath, cancellationToken);
        }

        public void DeleteFileIfExists(string path) => this.inner.DeleteFileIfExists(path);

        public void DeleteDirectoryIfExists(string path, bool recursive)
            => this.inner.DeleteDirectoryIfExists(path, recursive);

        public bool TryDeleteEmptyDirectory(string path) => this.inner.TryDeleteEmptyDirectory(path);

        public Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken)
        {
            this.onReadAllLinesAsync?.Invoke(path);
            return this.inner.ReadAllLinesAsync(path, cancellationToken);
        }

        public Task WriteAllLinesAtomicallyAsync(string path, IEnumerable<string> lines, CancellationToken cancellationToken)
        {
            this.onWriteAllLinesAtomicallyAsync?.Invoke(path, lines, cancellationToken);
            return this.inner.WriteAllLinesAtomicallyAsync(path, lines, cancellationToken);
        }

        public Task ReplaceFileAtomicallyAsync(
            string destinationPath,
            Func<string, CancellationToken, Task> writeTempFileAsync,
            CancellationToken cancellationToken)
        {
            this.onReplaceFileAtomicallyAsync?.Invoke(destinationPath, writeTempFileAsync, cancellationToken);
            return this.inner.ReplaceFileAtomicallyAsync(destinationPath, writeTempFileAsync, cancellationToken);
        }
    }
}
