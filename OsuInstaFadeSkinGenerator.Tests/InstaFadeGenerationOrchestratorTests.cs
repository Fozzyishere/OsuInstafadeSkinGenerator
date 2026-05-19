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
    public async Task GenerateAsync_CancelledDuringSdVariant_FinishesSdVariantBeforeReturningCancelled()
    {
        using var skinDir = new TestSkinDirectory();
        var fixture = new SkinFixtureBuilder(skinDir)
            .FromTemplate(2)
            .WithStandardBaseAssets()
            .WithStandardSdNumberAssets()
            .Build();

        using var cts = new CancellationTokenSource();
        var sdVisibleWriteCount = 0;
        var fileSystem = new ThrowingFileSystem(
            new PhysicalFileSystem(),
            onReplaceFileAtomicallyAsync: (destinationPath, _, _) =>
            {
                var fileName = Path.GetFileName(destinationPath);
                if (!fileName.StartsWith($"{fixture.Prefix}-", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                sdVisibleWriteCount++;
                if (sdVisibleWriteCount == 3)
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
        AssertVariantCompleted(fixture.RootPath, fixture.Prefix, string.Empty, 5, SkinFixtureBuilder.DefaultBaseAssetSize);
    }

    [Fact]
    public async Task GenerateAsync_CancelledDuringHdVariant_FinishesHdVariantBeforeReturningCancelled()
    {
        using var skinDir = new TestSkinDirectory();
        var fixture = new SkinFixtureBuilder(skinDir)
            .FromTemplate(4)
            .WithStandardBaseAssets()
            .WithHdBaseAssets(HdBaseAssetSize, HdHitcircleColor, HdOverlayColor)
            .WithStandardSdNumberAssets()
            .Build();

        using var cts = new CancellationTokenSource();
        var hdVisibleWriteCount = 0;
        var fileSystem = new ThrowingFileSystem(
            new PhysicalFileSystem(),
            onReplaceFileAtomicallyAsync: (destinationPath, _, _) =>
            {
                var fileName = Path.GetFileName(destinationPath);
                if (!fileName.Contains(SkinAssetNames.HdSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                hdVisibleWriteCount++;
                if (hdVisibleWriteCount == 3)
                {
                    cts.Cancel();
                }
            });

        var orchestrator = CreateOrchestratorWith(fileSystem);

        var result = await orchestrator.GenerateAsync(
            CreateRequest(fixture.RootPath, processHd: true),
            cancellationToken: cts.Token);

        Assert.Equal(GenerationStatus.Cancelled, result.Status);
        Assert.Equal(
            SkinIniTemplateFixture.GetTemplateContent(fixture.TemplateNumber),
            File.ReadAllText(Path.Combine(fixture.RootPath, SkinAssetNames.SkinIni)));
        AssertVariantCompleted(fixture.RootPath, fixture.Prefix, string.Empty, 5, SkinFixtureBuilder.DefaultBaseAssetSize);
        AssertVariantCompleted(fixture.RootPath, fixture.Prefix, SkinAssetNames.HdSuffix, 10, HdBaseAssetSize);
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

        public ThrowingFileSystem(
            IFileSystem inner,
            Action<string>? onDirectoryExists = null,
            Action<string>? onFileExists = null,
            Action<string>? onReadAllLinesAsync = null,
            Action<string, IEnumerable<string>, CancellationToken>? onWriteAllLinesAtomicallyAsync = null,
            Action<string, Func<string, CancellationToken, Task>, CancellationToken>? onReplaceFileAtomicallyAsync = null)
        {
            this.inner = inner;
            this.onDirectoryExists = onDirectoryExists;
            this.onFileExists = onFileExists;
            this.onReadAllLinesAsync = onReadAllLinesAsync;
            this.onWriteAllLinesAtomicallyAsync = onWriteAllLinesAtomicallyAsync;
            this.onReplaceFileAtomicallyAsync = onReplaceFileAtomicallyAsync;
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

        public void CopyFile(string sourcePath, string destinationPath, bool overwrite)
            => this.inner.CopyFile(sourcePath, destinationPath, overwrite);

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
