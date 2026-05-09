using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OsuInstaFadeSkinGenerator.Application.Ports;

public interface IFileSystem
{
    bool FileExists(string path);

    bool DirectoryExists(string path);

    void CreateDirectory(string path);

    void CopyFile(string sourcePath, string destinationPath, bool overwrite);

    Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken);

    Task WriteAllLinesAtomicallyAsync(string path, IEnumerable<string> lines, CancellationToken cancellationToken);

    Task ReplaceFileAtomicallyAsync(
        string destinationPath,
        Func<string, CancellationToken, Task> writeTempFileAsync,
        CancellationToken cancellationToken);
}
