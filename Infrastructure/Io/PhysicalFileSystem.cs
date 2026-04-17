using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OsuInstaFadeSkinGenerator.Application.Ports;

namespace OsuInstaFadeSkinGenerator.Infrastructure.Io;

public sealed class PhysicalFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite) =>
        File.Copy(sourcePath, destinationPath, overwrite);

    public Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken) =>
        File.ReadAllLinesAsync(path, cancellationToken);

    public Task WriteAllLinesAsync(string path, IEnumerable<string> lines, CancellationToken cancellationToken) =>
        File.WriteAllLinesAsync(path, lines, cancellationToken);
}
