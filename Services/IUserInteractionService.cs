namespace OsuInstaFadeSkinGenerator.Services;

public interface IUserInteractionService
{
    Task<string?> PickSkinFolderAsync();

    Task<bool> ConfirmGenerationAsync();

    Task SetClipboardTextAsync(string text);
}
