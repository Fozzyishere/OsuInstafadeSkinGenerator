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

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite) =>
        File.Copy(sourcePath, destinationPath, overwrite);

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
        var directory = Path.GetDirectoryName(fullDestination);
        if (string.IsNullOrEmpty(directory))
        {
            throw new ArgumentException("Destination path does not have a valid directory.", nameof(destinationPath));
        }

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
