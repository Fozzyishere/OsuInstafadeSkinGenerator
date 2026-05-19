using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OsuInstaFadeSkinGenerator.Application.Ports;

namespace OsuInstaFadeSkinGenerator.Infrastructure.Io;

public sealed class PhysicalFileSystem : IFileSystem
{
    private readonly ILogger<PhysicalFileSystem> logger;

    public PhysicalFileSystem()
        : this(NullLogger<PhysicalFileSystem>.Instance)
    {
    }

    public PhysicalFileSystem(ILogger<PhysicalFileSystem> logger)
    {
        this.logger = logger;
    }

    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public string CreateTemporaryDirectory(string parentDirectory, string prefix)
    {
        var directory = Path.Combine(parentDirectory, $"{prefix}{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite) =>
        File.Copy(sourcePath, destinationPath, overwrite);

    public Task CopyFileAtomicallyAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken) =>
        this.ReplaceFileAtomicallyAsync(
            destinationPath,
            async (tempPath, ct) =>
            {
                await using var source = new FileStream(
                    sourcePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 81920,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                await using var destination = new FileStream(
                    tempPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);

                await source.CopyToAsync(destination, ct).ConfigureAwait(false);
            },
            cancellationToken);

    public void DeleteFileIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public void DeleteDirectoryIfExists(string path, bool recursive)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive);
        }
    }

    public bool TryDeleteEmptyDirectory(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                return true;
            }

            Directory.Delete(path, recursive: false);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken) =>
        File.ReadAllLinesAsync(path, cancellationToken);

    public Task WriteAllLinesAtomicallyAsync(string path, IEnumerable<string> lines, CancellationToken cancellationToken) =>
        this.ReplaceFileAtomicallyAsync(
            path,
            (tempPath, ct) => File.WriteAllLinesAsync(tempPath, lines, ct),
            cancellationToken);

    public async Task ReplaceFileAtomicallyAsync(
        string destinationPath,
        Func<string, CancellationToken, Task> writeTempFileAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destinationPath);
        ArgumentNullException.ThrowIfNull(writeTempFileAsync);

        var fullDestination = Path.GetFullPath(destinationPath);
        var directory = Path.GetDirectoryName(fullDestination) ?? Directory.GetCurrentDirectory();

        var fileName = Path.GetFileName(fullDestination);
        var tempPath = Path.Combine(directory, $"{fileName}.{Guid.NewGuid():N}.tmp");

        var moved = false;
        try
        {
            await writeTempFileAsync(tempPath, cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, fullDestination, overwrite: true);
            moved = true;
        }
        finally
        {
            if (!moved)
            {
                this.TryDeleteQuietly(tempPath);
            }
        }
    }

    private void TryDeleteQuietly(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to delete temp file {Path}", path);
        }
    }
}
