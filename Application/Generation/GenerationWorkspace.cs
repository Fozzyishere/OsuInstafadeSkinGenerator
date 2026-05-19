using System;
using System.IO;
using OsuInstaFadeSkinGenerator.Application.Ports;
using OsuInstaFadeSkinGenerator.Domain;

namespace OsuInstaFadeSkinGenerator.Application.Generation;

internal sealed class GenerationWorkspace : IDisposable
{
    private const string WorkspacePrefix = ".insta-fade-work-";
    private readonly IFileSystem fileSystem;
    private bool disposed;

    private GenerationWorkspace(IFileSystem fileSystem, string rootPath)
    {
        this.fileSystem = fileSystem;
        this.RootPath = rootPath;
        this.StagingPath = Path.Combine(rootPath, "staging");
        this.SnapshotPath = Path.Combine(rootPath, "snapshot");
        this.fileSystem.CreateDirectory(this.StagingPath);
        this.fileSystem.CreateDirectory(this.SnapshotPath);
    }

    public string RootPath { get; }

    public string StagingPath { get; }

    public string SnapshotPath { get; }

    public static GenerationWorkspace Create(string skinFolder, IFileSystem fileSystem)
    {
        var rootPath = ResilientFileOperations.Run(
            () => fileSystem.CreateTemporaryDirectory(skinFolder, WorkspacePrefix),
            GenerationError.IoFailure,
            "create generation workspace");
        return new GenerationWorkspace(fileSystem, rootPath);
    }

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        try
        {
            this.fileSystem.DeleteDirectoryIfExists(this.RootPath, recursive: true);
        }
        catch
        {
            // Workspace cleanup should never turn a completed restore into a user-visible failure.
        }
    }
}
