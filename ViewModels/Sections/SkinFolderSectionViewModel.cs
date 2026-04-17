using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsuInstaFadeSkinGenerator.Application.Ports;
using OsuInstaFadeSkinGenerator.Application.Validation;
using OsuInstaFadeSkinGenerator.Domain;

namespace OsuInstaFadeSkinGenerator.ViewModels.Sections;

public sealed partial class SkinFolderSectionViewModel : ObservableObject
{
    private readonly IInputValidationService inputValidationService;
    private readonly ISkinIniReader skinIniReader;
    private readonly IFilePickerService filePickerService;
    private string lastSubmittedSkinFolderPath = string.Empty;
    private string loadedSkinFolderPath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPendingConfirmation))]
    private string skinFolderPath = string.Empty;

    [ObservableProperty]
    private string activeSkinFolderPath = string.Empty;

    public SkinFolderSectionViewModel(
        IInputValidationService inputValidationService,
        ISkinIniReader skinIniReader,
        IFilePickerService filePickerService)
    {
        this.inputValidationService = inputValidationService;
        this.skinIniReader = skinIniReader;
        this.filePickerService = filePickerService;
    }

    public event EventHandler<SkinConfig>? FolderLoaded;

    public event EventHandler? FolderCleared;

    public event EventHandler<string?>? ErrorChanged;

    public event EventHandler<string>? LogRequested;

    public bool HasPendingConfirmation =>
        !string.Equals(this.SkinFolderPath, this.lastSubmittedSkinFolderPath, StringComparison.Ordinal);

    [RelayCommand]
    private async Task BrowseAsync()
    {
        try
        {
            var path = await this.filePickerService.PickSkinFolderAsync();
            if (path == null)
            {
                return;
            }

            this.SkinFolderPath = path;
            await this.ConfirmInternalAsync(logPath: true).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            this.RaiseError($"Failed to select skin folder: {ex.Message}");
        }
    }

    [RelayCommand]
    private Task ConfirmSkinFolderPathAsync() => this.ConfirmInternalAsync(logPath: false);

    private async Task ConfirmInternalAsync(bool logPath)
    {
        this.lastSubmittedSkinFolderPath = this.SkinFolderPath;
        this.OnPropertyChanged(nameof(this.HasPendingConfirmation));

        switch (this.inputValidationService.ValidateSkinFolder(this.SkinFolderPath, requireValue: false))
        {
            case SkinFolderValidation.Invalid invalid:
                this.ActiveSkinFolderPath = string.Empty;
                this.loadedSkinFolderPath = string.Empty;
                this.FolderCleared?.Invoke(this, EventArgs.Empty);
                this.RaiseError(invalid.Message, writeToLog: true);
                return;

            case SkinFolderValidation.Empty:
                this.ActiveSkinFolderPath = string.Empty;
                this.loadedSkinFolderPath = string.Empty;
                this.FolderCleared?.Invoke(this, EventArgs.Empty);
                this.ClearError();
                return;

            case SkinFolderValidation.Valid valid:
                this.ClearError();
                await this.LoadSkinFolderAsync(valid, logPath).ConfigureAwait(true);
                return;
        }
    }

    private async Task LoadSkinFolderAsync(SkinFolderValidation.Valid valid, bool logPath)
    {
        this.ActiveSkinFolderPath = valid.SkinFolderPath;

        if (string.Equals(this.loadedSkinFolderPath, valid.SkinFolderPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        this.FolderCleared?.Invoke(this, EventArgs.Empty);

        if (logPath)
        {
            this.LogRequested?.Invoke(this, $"Selected skin folder: {valid.SkinFolderPath}");
        }

        try
        {
            var config = await this.skinIniReader.ReadAsync(valid.SkinIniPath, CancellationToken.None).ConfigureAwait(true);
            this.loadedSkinFolderPath = valid.SkinFolderPath;
            this.FolderLoaded?.Invoke(this, config);
        }
        catch (Exception ex)
        {
            this.ActiveSkinFolderPath = string.Empty;
            this.loadedSkinFolderPath = string.Empty;
            this.FolderCleared?.Invoke(this, EventArgs.Empty);
            this.RaiseError($"Failed to read skin.ini: {ex.Message}", writeToLog: true);
        }
    }

    private void ClearError()
    {
        this.ErrorChanged?.Invoke(this, null);
    }

    private void RaiseError(string message, bool writeToLog = false)
    {
        if (writeToLog)
        {
            this.LogRequested?.Invoke(this, $"ERROR: {message}");
        }

        this.ErrorChanged?.Invoke(this, message);
    }
}
