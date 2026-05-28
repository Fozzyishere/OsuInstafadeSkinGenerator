using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OsuInstaFadeSkinGenerator.Application.Ports;
using OsuInstaFadeSkinGenerator.Domain;

namespace OsuInstaFadeSkinGenerator.Application.Generation;

internal sealed class GenerationTransaction : IDisposable
{
    private readonly IFileSystem fileSystem;
    private readonly string skinFolder;
    private readonly GenerationWorkspace workspace;
    private readonly List<GenerationFileChange> changes = [];
    private readonly HashSet<string> targetPaths;
    private readonly List<string> createdTargetDirectories = [];
    private bool disposed;

    private GenerationTransaction(string skinFolder, IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem;
        this.skinFolder = Path.GetFullPath(skinFolder);
        this.workspace = GenerationWorkspace.Create(this.skinFolder, fileSystem);
        this.targetPaths = new HashSet<string>(GetPathComparer());
    }

    public static GenerationTransaction Create(string skinFolder, IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(skinFolder);
        if (string.IsNullOrWhiteSpace(skinFolder))
        {
            throw new ArgumentException("Skin folder path must not be empty or whitespace.", nameof(skinFolder));
        }

        ArgumentNullException.ThrowIfNull(fileSystem);

        return new GenerationTransaction(skinFolder, fileSystem);
    }

    public string CreateStagedPathForTarget(string targetPath)
    {
        var fullTargetPath = Path.GetFullPath(targetPath);
        SkinPathResolver.ThrowIfOutsideSkinFolder(
            this.skinFolder,
            fullTargetPath,
            GenerationError.UnsafeOutputPath,
            $"Generation would write outside the selected skin folder: '{SkinPathResolver.GetDisplayPath(targetPath)}'.");

        if (!this.targetPaths.Add(fullTargetPath))
        {
            throw new GenerationFailureException(
                GenerationError.IoFailure,
                $"Generation would write '{SkinPathResolver.GetDisplayPath(targetPath)}' more than once.");
        }

        var extension = Path.GetExtension(fullTargetPath);
        var stagedPath = Path.Combine(this.workspace.StagingPath, $"{this.changes.Count:D4}{extension}");
        this.changes.Add(new GenerationFileChange(fullTargetPath, stagedPath));
        return stagedPath;
    }

    public async Task<bool> CommitAsync(IProgress<GenerationProgress>? progress, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        if (!await this.SnapshotTargetsAsync(cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        try
        {
            foreach (var change in this.changes)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    await this.RollBackAfterCommitFailureAsync(progress, null).ConfigureAwait(false);
                    return false;
                }

                this.EnsureTargetParentDirectory(change.TargetPath);
                await ResilientFileOperations.RunAsync(
                    () => this.fileSystem.CopyFileAtomicallyAsync(change.StagedPath, change.TargetPath, CancellationToken.None),
                    GenerationError.IoFailure,
                    $"write {SkinPathResolver.GetDisplayPath(change.TargetPath)}").ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                {
                    await this.RollBackAfterCommitFailureAsync(progress, null).ConfigureAwait(false);
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            await this.RollBackAfterCommitFailureAsync(progress, ex).ConfigureAwait(false);
            throw;
        }

        return true;
    }

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.workspace.Dispose();
    }

    private static StringComparer GetPathComparer() =>
        OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    private async Task<bool> SnapshotTargetsAsync(CancellationToken cancellationToken)
    {
        try
        {
            for (var i = 0; i < this.changes.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                var change = this.changes[i];
                change.OriginalExists = this.fileSystem.FileExists(change.TargetPath);
                if (!change.OriginalExists)
                {
                    continue;
                }

                var snapshotPath = Path.Combine(this.workspace.SnapshotPath, $"{i:D4}{Path.GetExtension(change.TargetPath)}");
                await ResilientFileOperations.RunAsync(
                    () => this.fileSystem.CopyFileAtomicallyAsync(change.TargetPath, snapshotPath, cancellationToken),
                    GenerationError.IoFailure,
                    $"snapshot {SkinPathResolver.GetDisplayPath(change.TargetPath)}").ConfigureAwait(false);
                change.SnapshotPath = snapshotPath;

                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }
            }

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private async Task RollBackAfterCommitFailureAsync(IProgress<GenerationProgress>? progress, Exception? originalException)
    {
        progress?.Report(new GenerationProgress(
            GenerationPhase.RollingBack,
            0,
            "Restoring original files..."));

        try
        {
            await this.RollbackAsync().ConfigureAwait(false);
        }
        catch (Exception rollbackException)
        {
            var innerException = originalException == null
                ? rollbackException
                : new AggregateException(originalException, rollbackException);

            throw new GenerationFailureException(
                GenerationError.RollbackFailed,
                "Generation was interrupted, and restoring the original files failed. Manual restore may be required.",
                innerException);
        }
    }

    private async Task RollbackAsync()
    {
        foreach (var change in this.changes.AsEnumerable().Reverse())
        {
            if (change.OriginalExists)
            {
                if (change.SnapshotPath == null)
                {
                    throw new InvalidOperationException($"Missing rollback snapshot for '{change.TargetPath}'.");
                }

                this.EnsureTargetParentDirectory(change.TargetPath);
                await ResilientFileOperations.RunAsync(
                    () => this.fileSystem.CopyFileAtomicallyAsync(change.SnapshotPath, change.TargetPath, CancellationToken.None),
                    GenerationError.RollbackFailed,
                    $"restore {SkinPathResolver.GetDisplayPath(change.TargetPath)}").ConfigureAwait(false);
            }
            else
            {
                ResilientFileOperations.Run(
                    () => this.fileSystem.DeleteFileIfExists(change.TargetPath),
                    GenerationError.RollbackFailed,
                    $"remove generated {SkinPathResolver.GetDisplayPath(change.TargetPath)}");
            }
        }

        foreach (var directory in this.createdTargetDirectories
                     .Distinct(GetPathComparer())
                     .OrderByDescending(path => path.Length))
        {
            this.fileSystem.TryDeleteEmptyDirectory(directory);
        }
    }

    private void EnsureTargetParentDirectory(string targetPath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(targetPath));
        if (string.IsNullOrEmpty(directory) || this.fileSystem.DirectoryExists(directory))
        {
            return;
        }

        var missingDirectories = new Stack<string>();
        var current = directory;
        while (!string.IsNullOrEmpty(current) && !this.fileSystem.DirectoryExists(current))
        {
            missingDirectories.Push(current);
            current = Path.GetDirectoryName(current);
        }

        ResilientFileOperations.Run(
            () => this.fileSystem.CreateDirectory(directory),
            GenerationError.IoFailure,
            $"create folder for {SkinPathResolver.GetDisplayPath(targetPath)}");

        while (missingDirectories.Count > 0)
        {
            this.createdTargetDirectories.Add(missingDirectories.Pop());
        }
    }
}
