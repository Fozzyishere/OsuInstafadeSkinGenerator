using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsuInstaFadeSkinGenerator.Application.Generation;
using OsuInstaFadeSkinGenerator.Application.Ports;
using OsuInstaFadeSkinGenerator.Infrastructure.Imaging;
using OsuInstaFadeSkinGenerator.Infrastructure.Io;
using OsuInstaFadeSkinGenerator.Infrastructure.SkinIni;
using OsuInstaFadeSkinGenerator.Models;
using OsuInstaFadeSkinGenerator.Services;
using OsuInstaFadeSkinGenerator.ViewModels.Sections;

namespace OsuInstaFadeSkinGenerator.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private const string SkinFolderValidationKey = "SkinFolder";
    private const string ColourValidationKey = "Colour";
    private const string ClipboardValidationKey = "Clipboard";
    private const string GenerationValidationKey = "Generation";

    private readonly IInputValidationService inputValidationService;
    private readonly IGenerationService generationService;
    private readonly IUserInteractionService userInteractionService;
    private readonly Dictionary<string, string> validationErrors = [];
    private CancellationTokenSource? generationCts;

    [ObservableProperty]
    private string errorText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGenerate))]
    [NotifyCanExecuteChangedFor(nameof(GenerateCommand))]
    private bool hasErrors;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInputEnabled))]
    [NotifyPropertyChangedFor(nameof(CanGenerate))]
    [NotifyCanExecuteChangedFor(nameof(GenerateCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool isGenerating;

    public MainWindowViewModel(
        IInputValidationService inputValidationService,
        IGenerationService generationService,
        IUserInteractionService userInteractionService,
        SkinFolderSectionViewModel folder,
        ColourSectionViewModel colour,
        GenerationOptionsViewModel options,
        GenerationLogViewModel log)
    {
        this.inputValidationService = inputValidationService;
        this.generationService = generationService;
        this.userInteractionService = userInteractionService;
        this.Folder = folder;
        this.Colour = colour;
        this.Options = options;
        this.Log = log;

        this.Log.CopySucceeded += this.OnLogCopySucceeded;
        this.Log.CopyFailed += this.OnLogCopyFailed;
        this.Colour.ErrorChanged += this.OnColourErrorChanged;
        this.Colour.PropertyChanged += this.OnColourPropertyChanged;
        this.Folder.ErrorChanged += this.OnFolderErrorChanged;
        this.Folder.LogRequested += this.OnFolderLogRequested;
        this.Folder.FolderLoaded += this.OnFolderLoaded;
        this.Folder.FolderCleared += this.OnFolderCleared;
        this.Folder.PropertyChanged += this.OnFolderPropertyChanged;
    }

    public SkinFolderSectionViewModel Folder { get; }

    public ColourSectionViewModel Colour { get; }

    public GenerationOptionsViewModel Options { get; }

    public GenerationLogViewModel Log { get; }

    public bool IsInputEnabled => !this.IsGenerating;

    public bool CanGenerate =>
        !this.IsGenerating
        && !this.HasErrors
        && !this.Folder.HasPendingConfirmation
        && !this.Colour.HasPendingConfirmation
        && this.Colour.AppliedColour != null
        && !string.IsNullOrWhiteSpace(this.Folder.ActiveSkinFolderPath);

    public static MainWindowViewModel CreateDesignTime()
    {
        return CreateDesignTime(new WindowInteractionService());
    }

    public static MainWindowViewModel CreateDesignTime(IUserInteractionService userInteractionService)
    {
        var inputValidationService = new InputValidationService();
        var fileSystem = new PhysicalFileSystem();
        var skinIniReader = new SkinIniReader(fileSystem);
        var skinIniWriter = new SkinIniWriter(fileSystem);
        var imageIo = new ImageSharpImageIo();

        return new MainWindowViewModel(
            inputValidationService,
            new InstaFadeGenerationOrchestrator(skinIniReader, skinIniWriter, fileSystem, imageIo),
            userInteractionService,
            new SkinFolderSectionViewModel(inputValidationService, skinIniReader, userInteractionService),
            new ColourSectionViewModel(inputValidationService),
            new GenerationOptionsViewModel(),
            new GenerationLogViewModel(userInteractionService));
    }

    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task GenerateAsync()
    {
        if (this.Colour.AppliedColour == null)
        {
            return;
        }

        var request = new GenerationRequest(
            this.Folder.ActiveSkinFolderPath,
            this.Colour.AppliedColour.Value,
            this.Options.ProcessHd,
            this.Options.BackupFiles,
            this.Options.EnableTripleStacking);

        this.ClearValidationError(GenerationValidationKey);

        var shouldProceed = await this.userInteractionService.ConfirmGenerationAsync();
        if (!shouldProceed)
        {
            this.Log.Append("Generation cancelled by user.");
            return;
        }

        this.SetGenerating(true);
        this.generationCts = new CancellationTokenSource();

        try
        {
            var progress = new Progress<GenerationProgress>(this.Log.ReportProgress);
            var outcome = await this.generationService.GenerateAsync(request, progress, this.generationCts.Token);

            this.Log.ProgressValue = 100;
            switch (outcome.Status)
            {
                case GenerationStatus.Succeeded:
                    this.ClearValidationError(GenerationValidationKey);
                    if (!string.IsNullOrEmpty(outcome.DetailMessage))
                    {
                        this.Log.Append(outcome.DetailMessage);
                    }

                    this.Log.Append("All done!");
                    break;
                case GenerationStatus.Cancelled:
                    this.Log.Append(outcome.DetailMessage ?? "Generation cancelled.");
                    break;
                case GenerationStatus.Failed:
                    this.SetValidationError(GenerationValidationKey, outcome.DetailMessage ?? "Generation failed.");
                    break;
            }
        }
        catch (Exception ex)
        {
            this.SetValidationError(GenerationValidationKey, $"Generation failed unexpectedly: {ex.Message}");
        }
        finally
        {
            this.generationCts?.Dispose();
            this.generationCts = null;
            this.SetGenerating(false);
        }
    }

    [RelayCommand(CanExecute = nameof(IsGenerating))]
    private void Cancel()
    {
        this.generationCts?.Cancel();
    }

    private void OnLogCopySucceeded(object? sender, EventArgs e)
    {
        this.ClearValidationError(ClipboardValidationKey);
    }

    private void OnLogCopyFailed(object? sender, string message)
    {
        this.SetValidationError(ClipboardValidationKey, message, writeToLog: false);
    }

    private void OnColourErrorChanged(object? sender, string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            this.ClearValidationError(ColourValidationKey);
        }
        else
        {
            this.SetValidationError(ColourValidationKey, message, writeToLog: false);
        }
    }

    private void OnColourPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ColourSectionViewModel.HasPendingConfirmation)
            or nameof(ColourSectionViewModel.AppliedColour))
        {
            this.RefreshGenerateAvailability();
        }
    }

    private void OnFolderErrorChanged(object? sender, string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            this.ClearValidationError(SkinFolderValidationKey);
        }
        else
        {
            this.SetValidationError(SkinFolderValidationKey, message, writeToLog: false);
        }
    }

    private void OnFolderLogRequested(object? sender, string message)
    {
        this.Log.Append(message);
    }

    private void OnFolderLoaded(object? sender, SkinConfig config)
    {
        this.Colour.ApplyFromComboColour(this.inputValidationService.GetPrimaryComboColour(config));
        this.Log.Append($"  HitCirclePrefix: {config.HitCirclePrefix}");

        if (config.ComboColours.Count > 0)
        {
            var comboColour = config.ComboColours.OrderBy(colour => colour.Index).First().Color;
            this.Log.Append($"  Current Combo1: {comboColour.R},{comboColour.G},{comboColour.B}");
        }
        else
        {
            this.Colour.ReportMissingComboColour("skin.ini does not define a combo colour. Enter one in RGB or hex, then press Apply.");
        }
    }

    private void OnFolderCleared(object? sender, EventArgs e)
    {
        this.Colour.ClearColourInputs();
    }

    private void OnFolderPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SkinFolderSectionViewModel.HasPendingConfirmation)
            or nameof(SkinFolderSectionViewModel.ActiveSkinFolderPath))
        {
            this.RefreshGenerateAvailability();
        }
    }

    private void SetGenerating(bool running)
    {
        this.IsGenerating = running;
        if (running)
        {
            this.Log.ProgressValue = 0;
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
            this.Log.Append($"ERROR: {message}");
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

    private void RefreshGenerateAvailability()
    {
        this.OnPropertyChanged(nameof(this.CanGenerate));
        this.GenerateCommand.NotifyCanExecuteChanged();
    }
}
