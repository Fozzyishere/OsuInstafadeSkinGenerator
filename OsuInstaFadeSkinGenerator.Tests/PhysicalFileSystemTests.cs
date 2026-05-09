using OsuInstaFadeSkinGenerator.Infrastructure.Io;

namespace OsuInstaFadeSkinGenerator.Tests;

public sealed class PhysicalFileSystemTests
{
    [Fact]
    public async Task WriteAllLinesAtomicallyAsync_ReplacesExistingFile_LeavesNoTempFiles()
    {
        using var dir = new TestSkinDirectory();
        var targetPath = Path.Combine(dir.RootPath, "target.txt");
        await File.WriteAllLinesAsync(targetPath, new[] { "old" });

        var fs = new PhysicalFileSystem();
        await fs.WriteAllLinesAtomicallyAsync(targetPath, new[] { "new", "lines" }, CancellationToken.None);

        Assert.Equal(new[] { "new", "lines" }, await File.ReadAllLinesAsync(targetPath));
        Assert.Empty(Directory.GetFiles(dir.RootPath, "*.tmp"));
    }

    [Fact]
    public async Task ReplaceFileAtomicallyAsync_WhenWriteFails_PreservesOriginalAndRemovesTemp()
    {
        using var dir = new TestSkinDirectory();
        var targetPath = Path.Combine(dir.RootPath, "data.bin");
        const string original = "original-bytes";
        await File.WriteAllTextAsync(targetPath, original);

        var fs = new PhysicalFileSystem();
        await Assert.ThrowsAsync<IOException>(
            async () => await fs.ReplaceFileAtomicallyAsync(
                    targetPath,
                    async (tempPath, ct) =>
                    {
                        await File.WriteAllTextAsync(tempPath, "partial", ct);
                        throw new IOException("simulated failure");
                    },
                    CancellationToken.None)
                .ConfigureAwait(false));

        Assert.Equal(original, await File.ReadAllTextAsync(targetPath));
        Assert.Empty(Directory.GetFiles(dir.RootPath, "*.tmp"));
    }

    [Fact]
    public async Task ReplaceFileAtomicallyAsync_CreatesFileWhenMissing()
    {
        using var dir = new TestSkinDirectory();
        var targetPath = Path.Combine(dir.RootPath, "new-file.txt");
        var fs = new PhysicalFileSystem();

        await fs.ReplaceFileAtomicallyAsync(
            targetPath,
            (tempPath, ct) => File.WriteAllTextAsync(tempPath, "created", ct),
            CancellationToken.None);

        Assert.Equal("created", await File.ReadAllTextAsync(targetPath));
        Assert.Empty(Directory.GetFiles(dir.RootPath, "*.tmp"));
    }

    [Fact]
    public async Task ReplaceFileAtomicallyAsync_WhenCancelledDuringWrite_ThrowsAndRemovesTemp()
    {
        using var dir = new TestSkinDirectory();
        var targetPath = Path.Combine(dir.RootPath, "cancelled.txt");
        await File.WriteAllTextAsync(targetPath, "original");

        var fs = new PhysicalFileSystem();
        using var cts = new CancellationTokenSource();

        var task = fs.ReplaceFileAtomicallyAsync(
            targetPath,
            async (tempPath, ct) =>
            {
                await File.WriteAllTextAsync(tempPath, "partial", ct);
                await Task.Delay(5).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
            },
            cts.Token);

        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task.ConfigureAwait(false));
        Assert.Equal("original", await File.ReadAllTextAsync(targetPath));
        Assert.Empty(Directory.GetFiles(dir.RootPath, "*.tmp"));
    }

    [Fact]
    public async Task ReplaceFileAtomicallyAsync_WhenDirectoryMissing_ThrowsAndLeavesNoTempFiles()
    {
        using var dir = new TestSkinDirectory();
        var missingDir = Path.Combine(dir.RootPath, "does-not-exist");
        var targetPath = Path.Combine(missingDir, "file.txt");
        var fs = new PhysicalFileSystem();

        await Assert.ThrowsAnyAsync<DirectoryNotFoundException>(async () =>
            await fs.ReplaceFileAtomicallyAsync(
                targetPath,
                (tempPath, ct) => File.WriteAllTextAsync(tempPath, "content", ct),
                CancellationToken.None).ConfigureAwait(false));

        Assert.False(Directory.Exists(missingDir));
        Assert.Empty(Directory.GetFiles(dir.RootPath, "*.tmp"));
    }

    [Fact]
    public async Task ReplaceFileAtomicallyAsync_WhenTargetLocked_ThrowsAndRemovesTemp()
    {
        using var dir = new TestSkinDirectory();
        var targetPath = Path.Combine(dir.RootPath, "locked.txt");
        await File.WriteAllTextAsync(targetPath, "original");

        var fs = new PhysicalFileSystem();
        Exception ex;
        using (var lockStream = new FileStream(
                   targetPath,
                   FileMode.Open,
                   FileAccess.Read,
                   FileShare.None))
        {
            ex = await Assert.ThrowsAnyAsync<Exception>(async () =>
                await fs.ReplaceFileAtomicallyAsync(
                    targetPath,
                    (tempPath, ct) => File.WriteAllTextAsync(tempPath, "new", ct),
                    CancellationToken.None).ConfigureAwait(false));
        }

        Assert.True(ex is IOException or UnauthorizedAccessException, $"Expected IOException or UnauthorizedAccessException, got {ex.GetType().Name}.");

        Assert.Equal("original", await File.ReadAllTextAsync(targetPath));
        Assert.Empty(Directory.GetFiles(dir.RootPath, "*.tmp"));
    }
}
