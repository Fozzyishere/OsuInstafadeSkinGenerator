using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using OsuInstaFadeSkinGenerator.Application.Ports;

namespace OsuInstaFadeSkinGenerator.Infrastructure.Avalonia;

public sealed class AvaloniaFilePickerService : IFilePickerService
{
    private readonly IOwnerWindowProvider ownerWindowProvider;
    private readonly ILogger<AvaloniaFilePickerService> logger;

    public AvaloniaFilePickerService(
        IOwnerWindowProvider ownerWindowProvider,
        ILogger<AvaloniaFilePickerService> logger)
    {
        this.ownerWindowProvider = ownerWindowProvider;
        this.logger = logger;
    }

    public async Task<string?> PickSkinFolderAsync(CancellationToken cancellationToken = default)
    {
        var owner = this.ownerWindowProvider.Current
            ?? throw new InvalidOperationException("No owner window is registered; cannot show the folder picker.");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var folders = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select osu! Skin Folder",
                AllowMultiple = false,
            }).ConfigureAwait(true);

            return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            this.logger.LogError(ex, "Skin folder picker failed.");
            throw;
        }
    }
}
