using Avalonia.Controls;
using Avalonia.Platform.Storage;
using OsuInstaFadeSkinGenerator.Views.Dialogs;

namespace OsuInstaFadeSkinGenerator.Services;

public sealed class WindowInteractionService : IUserInteractionService
{
    private Window? owner;

    public void Attach(Window window)
    {
        this.owner = window;
    }

    public async Task<string?> PickSkinFolderAsync()
    {
        var folders = await this.GetOwner().StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select osu! Skin Folder",
            AllowMultiple = false,
        });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    public Task<bool> ConfirmGenerationAsync()
    {
        return new ConfirmGenerationWindow().ShowDialog<bool>(this.GetOwner());
    }

    public async Task SetClipboardTextAsync(string text)
    {
        var clipboard = this.GetOwner().Clipboard;
        if (clipboard == null)
        {
            throw new InvalidOperationException("Clipboard is not available on this system.");
        }

        await clipboard.SetTextAsync(text);
    }

    private Window GetOwner()
    {
        return this.owner ?? throw new InvalidOperationException("Window interactions are not attached to a window.");
    }
}
