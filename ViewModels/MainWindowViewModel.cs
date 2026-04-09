using Avalonia.Media;
using OsuInstaFadeSkinGenerator.Models;
using OsuInstaFadeSkinGenerator.Services;

namespace OsuInstaFadeSkinGenerator.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private const string SkinFolderValidationKey = "SkinFolder";
    private const string ColourValidationKey = "Colour";
    private const string ClipboardValidationKey = "Clipboard";
    private const string GenerationValidationKey = "Generation";
    private const string DefaultLogMessage = "Ready. Select a skin folder to get started.";

    private readonly IInputValidationService inputValidationService;
    private readonly ISkinIniReader skinIniReader;
    private readonly IGenerationService generationService;
    private readonly IUserInteractionService userInteractionService;
    private readonly Dictionary<string, string> validationErrors = [];
    private readonly List<string> logEntries = [];
    private ColourSelection? appliedColour;
    private bool isUpdatingColour;
    private bool isGenerating;
    private bool hasErrors;
    private bool hasLogEntries;
    private string lastSubmittedSkinFolderPath = string.Empty;
    private string loadedSkinFolderPath = string.Empty;
    private string activeSkinFolderPath = string.Empty;
    private string skinFolderPath = string.Empty;
    private string colourRText = string.Empty;
    private string colourGText = string.Empty;
    private string colourBText = string.Empty;
    private string colourHex = string.Empty;
    private IBrush colourPreviewBrush = Brushes.Transparent;
    private bool backupFiles = true;
    private bool processHd = true;
    private bool enableTripleStacking;
    private string errorText = string.Empty;
    private string logText = DefaultLogMessage;
    private double progressValue;

    public MainWindowViewModel(
        IInputValidationService inputValidationService,
        ISkinIniReader skinIniReader,
        IGenerationService generationService,
        IUserInteractionService userInteractionService)
    {
        this.inputValidationService = inputValidationService;
        this.skinIniReader = skinIniReader;
        this.generationService = generationService;
        this.userInteractionService = userInteractionService;

        this.BrowseCommand = new AsyncCommand(this.BrowseAsync, () => this.IsInputEnabled);
        this.ConfirmSkinFolderPathCommand = new RelayCommand(() => this.ConfirmSkinFolderPathInput(), () => this.IsInputEnabled);
        this.ApplyRgbCommand = new RelayCommand(this.ApplyRgbColour, () => this.IsInputEnabled);
        this.ApplyHexCommand = new RelayCommand(this.ApplyHexColour, () => this.IsInputEnabled);
        this.GenerateCommand = new AsyncCommand(this.GenerateAsync, () => this.CanGenerate);
        this.CopyLogsCommand = new AsyncCommand(this.CopyLogsAsync, () => this.HasLogEntries);
    }

    public string SkinFolderPath
    {
        get => this.skinFolderPath;
        set
        {
            if (this.SetProperty(ref this.skinFolderPath, value))
            {
                this.ClearValidationError(SkinFolderValidationKey);
                this.RefreshGenerateAvailability();
            }
        }
    }

    public string ColourRText
    {
        get => this.colourRText;
        set
        {
            if (this.SetProperty(ref this.colourRText, value))
            {
                this.OnColourDraftChanged();
            }
        }
    }

    public string ColourGText
    {
        get => this.colourGText;
        set
        {
            if (this.SetProperty(ref this.colourGText, value))
            {
                this.OnColourDraftChanged();
            }
        }
    }

    public string ColourBText
    {
        get => this.colourBText;
        set
        {
            if (this.SetProperty(ref this.colourBText, value))
            {
                this.OnColourDraftChanged();
            }
        }
    }

    public string ColourHex
    {
        get => this.colourHex;
        set
        {
            if (this.SetProperty(ref this.colourHex, value))
            {
                this.OnColourDraftChanged();
            }
        }
    }

    public IBrush ColourPreviewBrush
    {
        get => this.colourPreviewBrush;
        private set => this.SetProperty(ref this.colourPreviewBrush, value);
    }

    public bool BackupFiles
    {
        get => this.backupFiles;
        set => this.SetProperty(ref this.backupFiles, value);
    }

    public bool ProcessHd
    {
        get => this.processHd;
        set => this.SetProperty(ref this.processHd, value);
    }

    public bool EnableTripleStacking
    {
        get => this.enableTripleStacking;
        set => this.SetProperty(ref this.enableTripleStacking, value);
    }

    public string ErrorText
    {
        get => this.errorText;
        private set => this.SetProperty(ref this.errorText, value);
    }

    public string LogText
    {
        get => this.logText;
        private set => this.SetProperty(ref this.logText, value);
    }

    public double ProgressValue
    {
        get => this.progressValue;
        private set => this.SetProperty(ref this.progressValue, value);
    }

    public bool HasErrors
    {
        get => this.hasErrors;
        private set
        {
            if (this.SetProperty(ref this.hasErrors, value))
            {
                this.OnPropertyChanged(nameof(this.CanGenerate));
                this.RefreshCommandStates();
            }
        }
    }

    public bool HasLogEntries
    {
        get => this.hasLogEntries;
        private set
        {
            if (this.SetProperty(ref this.hasLogEntries, value))
            {
                this.RefreshCommandStates();
            }
        }
    }

    public bool IsGenerating
    {
        get => this.isGenerating;
        private set
        {
            if (this.SetProperty(ref this.isGenerating, value))
            {
                this.OnPropertyChanged(nameof(this.IsInputEnabled));
                this.OnPropertyChanged(nameof(this.CanGenerate));
                this.RefreshCommandStates();
            }
        }
    }

    public bool IsInputEnabled => !this.IsGenerating;

    public bool CanGenerate =>
        !this.IsGenerating
        && !this.HasErrors
        && !this.HasPendingSkinFolderConfirmation
        && !this.HasPendingColourConfirmation
        && this.appliedColour != null
        && !string.IsNullOrWhiteSpace(this.activeSkinFolderPath);

    public AsyncCommand BrowseCommand { get; }

    public RelayCommand ConfirmSkinFolderPathCommand { get; }

    public RelayCommand ApplyRgbCommand { get; }

    public RelayCommand ApplyHexCommand { get; }

    public AsyncCommand GenerateCommand { get; }

    public AsyncCommand CopyLogsCommand { get; }

    private bool HasPendingSkinFolderConfirmation =>
        !string.Equals(this.SkinFolderPath, this.lastSubmittedSkinFolderPath, StringComparison.Ordinal);

    private bool HasPendingColourConfirmation =>
        this.appliedColour == null
            ? !string.IsNullOrWhiteSpace(this.ColourRText)
                || !string.IsNullOrWhiteSpace(this.ColourGText)
                || !string.IsNullOrWhiteSpace(this.ColourBText)
                || !string.IsNullOrWhiteSpace(this.ColourHex)
            : !string.Equals(this.ColourRText, this.appliedColour.Value.R.ToString(), StringComparison.Ordinal)
                || !string.Equals(this.ColourGText, this.appliedColour.Value.G.ToString(), StringComparison.Ordinal)
                || !string.Equals(this.ColourBText, this.appliedColour.Value.B.ToString(), StringComparison.Ordinal)
                || !string.Equals(this.ColourHex, this.appliedColour.Value.Hex, StringComparison.Ordinal);

    public static MainWindowViewModel CreateDesignTime()
    {
        return CreateDesignTime(new WindowInteractionService());
    }

    public static MainWindowViewModel CreateDesignTime(IUserInteractionService userInteractionService)
    {
        var inputValidationService = new InputValidationService();
        var skinIniReader = new SkinIniReader();
        var skinIniWriter = new SkinIniWriter();

        return new MainWindowViewModel(
            inputValidationService,
            skinIniReader,
            new InstaFadeGenerator(skinIniReader, skinIniWriter),
            userInteractionService);
    }

    private void ConfirmSkinFolderPathInput(bool logPath = false)
    {
        this.lastSubmittedSkinFolderPath = this.SkinFolderPath;
        this.RefreshGenerateAvailability();

        var skinFolderValidation = this.inputValidationService.ValidateSkinFolder(this.SkinFolderPath, requireValue: false);
        this.ApplySkinFolderValidation(skinFolderValidation, clearWhenEmpty: true);

        if (!skinFolderValidation.IsValid)
        {
            this.activeSkinFolderPath = string.Empty;
            this.loadedSkinFolderPath = string.Empty;
            this.ClearColourInputs();
            this.RefreshGenerateAvailability();
            return;
        }

        var skinFolderPath = skinFolderValidation.SkinFolderPath!;
        var skinIniPath = skinFolderValidation.SkinIniPath!;
        this.activeSkinFolderPath = skinFolderPath;
        this.RefreshGenerateAvailability();

        if (string.Equals(this.loadedSkinFolderPath, skinFolderPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        this.ClearColourInputs();

        if (logPath)
        {
            this.Log($"Selected skin folder: {skinFolderPath}");
        }

        try
        {
            var config = this.skinIniReader.Read(skinIniPath);
            this.loadedSkinFolderPath = skinFolderPath;
            this.ApplyComboColour(config);
            this.Log($"  HitCirclePrefix: {config.HitCirclePrefix}");

            if (config.ComboColours.Count > 0)
            {
                var comboColour = config.ComboColours.OrderBy(colour => colour.Index).First();
                this.Log($"  Current Combo1: {comboColour.R},{comboColour.G},{comboColour.B}");
            }
            else
            {
                this.SetValidationError(ColourValidationKey, "skin.ini does not define a combo colour. Enter one in RGB or hex, then press Apply.", writeToLog: false);
            }
        }
        catch (Exception ex)
        {
            this.activeSkinFolderPath = string.Empty;
            this.loadedSkinFolderPath = string.Empty;
            this.ClearColourInputs();
            this.SetValidationError(SkinFolderValidationKey, $"Failed to read skin.ini: {ex.Message}");
            this.RefreshGenerateAvailability();
        }
    }

    private async Task BrowseAsync()
    {
        try
        {
            var path = await this.userInteractionService.PickSkinFolderAsync();
            if (path == null)
            {
                return;
            }

            this.SkinFolderPath = path;
            this.ConfirmSkinFolderPathInput(logPath: true);
        }
        catch (Exception ex)
        {
            this.SetValidationError(SkinFolderValidationKey, $"Failed to select skin folder: {ex.Message}");
        }
    }

    private void ApplyComboColour(SkinConfig config)
    {
        var comboColour = this.inputValidationService.GetPrimaryComboColour(config);
        if (comboColour == null)
        {
            this.ClearColourInputs();
            return;
        }

        this.CommitAppliedColour(comboColour.Value);
    }

    private void ClearColourInputs()
    {
        this.appliedColour = null;
        this.isUpdatingColour = true;
        try
        {
            this.ColourRText = string.Empty;
            this.ColourGText = string.Empty;
            this.ColourBText = string.Empty;
            this.ColourHex = string.Empty;
            this.ColourPreviewBrush = Brushes.Transparent;
        }
        finally
        {
            this.isUpdatingColour = false;
        }

        this.ClearValidationError(ColourValidationKey);
        this.RefreshGenerateAvailability();
    }

    private void OnColourDraftChanged()
    {
        if (this.isUpdatingColour)
        {
            return;
        }

        this.ClearValidationError(ColourValidationKey);
        this.RefreshGenerateAvailability();
    }

    private void ApplyRgbColour()
    {
        var validation = this.inputValidationService.ValidateRgbInput(
            this.ColourRText,
            this.ColourGText,
            this.ColourBText,
            requireValue: false);

        if (validation.ErrorMessage != null)
        {
            this.SetValidationError(ColourValidationKey, validation.ErrorMessage, writeToLog: false);
            return;
        }

        if (validation.Colour == null)
        {
            this.ClearColourInputs();
            return;
        }

        this.CommitAppliedColour(validation.Colour.Value);
    }

    private void ApplyHexColour()
    {
        var validation = this.inputValidationService.ValidateHexInput(this.ColourHex, requireValue: false);
        if (validation.ErrorMessage != null)
        {
            this.SetValidationError(ColourValidationKey, validation.ErrorMessage, writeToLog: false);
            return;
        }

        if (validation.Colour == null)
        {
            this.ClearColourInputs();
            return;
        }

        this.CommitAppliedColour(validation.Colour.Value);
    }

    private void CommitAppliedColour(ColourSelection colour)
    {
        this.appliedColour = colour;
        this.SetColourInputs(colour);
        this.ColourPreviewBrush = new SolidColorBrush(Color.FromRgb(colour.R, colour.G, colour.B));
        this.ClearValidationError(ColourValidationKey);
        this.RefreshGenerateAvailability();
    }

    private void SetColourInputs(ColourSelection colour)
    {
        this.isUpdatingColour = true;
        try
        {
            this.ColourRText = colour.R.ToString();
            this.ColourGText = colour.G.ToString();
            this.ColourBText = colour.B.ToString();
            this.ColourHex = colour.Hex;
            this.ColourPreviewBrush = new SolidColorBrush(Color.FromRgb(colour.R, colour.G, colour.B));
        }
        finally
        {
            this.isUpdatingColour = false;
        }
    }

    private async Task GenerateAsync()
    {
        if (this.IsGenerating)
        {
            return;
        }

        if (!this.TryBuildRequest(out var request))
        {
            return;
        }

        this.ClearValidationError(GenerationValidationKey);

        var shouldProceed = await this.userInteractionService.ConfirmGenerationAsync();
        if (!shouldProceed)
        {
            this.Log("Generation cancelled by user.");
            return;
        }

        this.SetGenerating(true);

        try
        {
            var progress = new Progress<GenerationProgress>(this.ReportProgress);
            var result = await this.generationService.GenerateAsync(request, progress);

            this.ProgressValue = 100;
            if (result.Success)
            {
                this.ClearValidationError(GenerationValidationKey);
                this.Log(result.Message);
                this.Log("All done!");
            }
            else
            {
                this.SetValidationError(GenerationValidationKey, result.Message);
            }
        }
        catch (Exception ex)
        {
            this.SetValidationError(GenerationValidationKey, $"Generation failed unexpectedly: {ex.Message}");
        }
        finally
        {
            this.SetGenerating(false);
        }
    }

    private bool TryBuildRequest(out GenerationRequest request)
    {
        request = default!;

        if (this.HasPendingSkinFolderConfirmation)
        {
            this.SetValidationError(SkinFolderValidationKey, "Press Enter to confirm the skin folder path before generating.", writeToLog: false);
            return false;
        }

        var skinFolderValidation = this.inputValidationService.ValidateSkinFolder(this.SkinFolderPath, requireValue: true);
        this.ApplySkinFolderValidation(skinFolderValidation, clearWhenEmpty: false);
        if (!skinFolderValidation.IsValid)
        {
            return false;
        }

        var skinFolderPath = skinFolderValidation.SkinFolderPath!;
        if (!string.Equals(this.activeSkinFolderPath, skinFolderPath, StringComparison.OrdinalIgnoreCase))
        {
            this.activeSkinFolderPath = skinFolderPath;
        }

        if (this.HasPendingColourConfirmation)
        {
            this.SetValidationError(ColourValidationKey, "Press Apply to confirm combo colour changes.", writeToLog: false);
            return false;
        }

        if (this.appliedColour == null)
        {
            this.SetValidationError(ColourValidationKey, "Enter a combo colour in RGB or hex, then press Apply.", writeToLog: false);
            return false;
        }

        var colour = this.appliedColour.Value;
        request = new GenerationRequest(
            skinFolderPath,
            colour.R,
            colour.G,
            colour.B,
            this.ProcessHd,
            this.BackupFiles,
            this.EnableTripleStacking);
        return true;
    }

    private void ReportProgress(GenerationProgress progress)
    {
        this.ProgressValue = progress.Progress * 100;
        this.Log(progress.Message);
    }

    private void SetGenerating(bool running)
    {
        this.IsGenerating = running;
        if (running)
        {
            this.ProgressValue = 0;
        }
    }

    private async Task CopyLogsAsync()
    {
        if (!this.HasLogEntries)
        {
            return;
        }

        try
        {
            await this.userInteractionService.SetClipboardTextAsync(this.LogText);
            this.ClearValidationError(ClipboardValidationKey);
        }
        catch (Exception ex)
        {
            this.SetValidationError(ClipboardValidationKey, $"Failed to copy logs: {ex.Message}", writeToLog: false);
        }
    }

    private void ApplySkinFolderValidation(SkinFolderValidationResult validation, bool clearWhenEmpty)
    {
        if (validation.ErrorMessage != null)
        {
            this.SetValidationError(SkinFolderValidationKey, validation.ErrorMessage);
        }
        else if (validation.HasValue || clearWhenEmpty)
        {
            this.ClearValidationError(SkinFolderValidationKey);
        }
    }

    private void SetValidationError(string key, string message, bool writeToLog = true)
    {
        if (this.validationErrors.TryGetValue(key, out var existingMessage) && existingMessage == message)
        {
            return;
        }

        this.validationErrors[key] = message;
        this.RefreshValidationSummary();

        if (writeToLog)
        {
            this.Log($"ERROR: {message}");
        }
    }

    private void ClearValidationError(string key)
    {
        if (this.validationErrors.Remove(key))
        {
            this.RefreshValidationSummary();
        }
    }

    private void RefreshValidationSummary()
    {
        this.HasErrors = this.validationErrors.Count > 0;
        this.ErrorText = this.HasErrors
            ? string.Join(Environment.NewLine, this.validationErrors.Values)
            : string.Empty;
    }

    private void Log(string message)
    {
        this.logEntries.Add(message);
        this.RefreshLogText();
    }

    private void RefreshLogText()
    {
        this.HasLogEntries = this.logEntries.Count > 0;
        this.LogText = this.HasLogEntries
            ? string.Join(Environment.NewLine, this.logEntries)
            : DefaultLogMessage;
    }

    private void RefreshCommandStates()
    {
        this.BrowseCommand.RaiseCanExecuteChanged();
        this.ConfirmSkinFolderPathCommand.RaiseCanExecuteChanged();
        this.ApplyRgbCommand.RaiseCanExecuteChanged();
        this.ApplyHexCommand.RaiseCanExecuteChanged();
        this.GenerateCommand.RaiseCanExecuteChanged();
        this.CopyLogsCommand.RaiseCanExecuteChanged();
    }

    private void RefreshGenerateAvailability()
    {
        this.OnPropertyChanged(nameof(this.CanGenerate));
        this.RefreshCommandStates();
    }
}
