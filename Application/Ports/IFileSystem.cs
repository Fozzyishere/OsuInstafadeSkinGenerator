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

    string CreateTemporaryDirectory(string parentDirectory, string prefix);

    void CopyFile(string sourcePath, string destinationPath, bool overwrite);

    Task CopyFileAtomicallyAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken);

    void DeleteFileIfExists(string path);

    void DeleteDirectoryIfExists(string path, bool recursive);

    bool TryDeleteEmptyDirectory(string path);

    Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken);

    Task WriteAllLinesAtomicallyAsync(string path, IEnumerable<string> lines, CancellationToken cancellationToken);

    Task ReplaceFileAtomicallyAsync(
        string destinationPath,
        Func<string, CancellationToken, Task> writeTempFileAsync,
        CancellationToken cancellationToken);
}
