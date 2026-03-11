using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using OsuInstaFadeSkinGenerator.Models;
using OsuInstaFadeSkinGenerator.Services;
using OsuInstaFadeSkinGenerator.Views.Dialogs;

namespace OsuInstaFadeSkinGenerator.Views;

public partial class MainWindow : Window
{
    private const string SkinFolderValidationKey = "SkinFolder";
    private const string ColourValidationKey = "Colour";

    // recursion safeguard
    private readonly LogState logState = new();
    private readonly ValidationState validationState = new();
    private bool updatingColour;
    private bool isGenerating;
    private string? loadedSkinFolderPath;

    public MainWindow()
    {
        this.InitializeComponent();
        this.ColourR!.TextChanged += this.ColourComponent_TextChanged;
        this.ColourG!.TextChanged += this.ColourComponent_TextChanged;
        this.ColourB!.TextChanged += this.ColourComponent_TextChanged;
        this.ClearColourInputs();
        this.UpdateValidationUi();
    }

    private async void BrowseButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var path = await this.PickSkinFolderAsync();
            if (path == null)
            {
                return;
            }

            this.SkinFolderTextBox!.Text = path;
            this.LoadSkinInfo(path, logPath: true);
        }
        catch (Exception ex)
        {
            this.SetValidationError(SkinFolderValidationKey, $"Failed to select skin folder: {ex.Message}");
        }
    }

    private void SkinFolderTextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        this.LoadSkinInfo(this.SkinFolderTextBox?.Text);
    }

    private void SkinFolderTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        this.LoadSkinInfo(this.SkinFolderTextBox?.Text);
        e.Handled = true;
    }

    private async Task<string?> PickSkinFolderAsync()
    {
        var folders = await this.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select osu! Skin Folder",
            AllowMultiple = false,
        });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    private void LoadSkinInfo(string? inputPath, bool logPath = false)
    {
        var skinFolderValidation = GenerationInputService.ValidateSkinFolder(inputPath, requireValue: false);
        this.ApplySkinFolderValidation(skinFolderValidation, clearWhenEmpty: true);

        if (!skinFolderValidation.IsValid)
        {
            this.loadedSkinFolderPath = null;
            this.ClearColourInputs();
            return;
        }

        var skinFolderPath = skinFolderValidation.SkinFolderPath!;
        var skinIniPath = skinFolderValidation.SkinIniPath!;

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
            var config = SkinIniParser.Parse(skinIniPath);
            this.loadedSkinFolderPath = skinFolderPath;
            this.ApplyComboColour(config);
            this.Log($"  HitCirclePrefix: {config.HitCirclePrefix}");

            if (config.ComboColours.Count > 0)
            {
                var c = config.ComboColours.OrderBy(colour => colour.Index).First();
                this.Log($"  Current Combo1: {c.R},{c.G},{c.B}");
            }
            else
            {
                this.SetValidationError(ColourValidationKey, "skin.ini does not define a combo colour. Enter one in RGB or hex.", writeToLog: false);
            }
        }
        catch (Exception ex)
        {
            this.loadedSkinFolderPath = null;
            this.ClearColourInputs();
            this.SetValidationError(SkinFolderValidationKey, $"Failed to read skin.ini: {ex.Message}");
        }
    }

    private void ApplyComboColour(SkinConfig config)
    {
        var comboColour = GenerationInputService.GetPrimaryComboColour(config);
        if (comboColour == null)
        {
            this.ClearColourInputs();
            return;
        }

        this.SetColourInputs(comboColour.Value);
    }

    private void ClearColourInputs()
    {
        this.updatingColour = true;
        try
        {
            if (this.ColourR != null)
            {
                this.ColourR.Text = string.Empty;
            }

            if (this.ColourG != null)
            {
                this.ColourG.Text = string.Empty;
            }

            if (this.ColourB != null)
            {
                this.ColourB.Text = string.Empty;
            }

            if (this.ColourHex != null)
            {
                this.ColourHex.Text = string.Empty;
            }

            if (this.ColourPreview != null)
            {
                this.ColourPreview.Background = Brushes.Transparent;
            }
        }
        finally
        {
            this.updatingColour = false;
        }
    }

    private void ColourComponent_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (this.updatingColour)
        {
            return;
        }

        this.updatingColour = true;
        try
        {
            this.UpdateColourPreview();
            this.UpdateHexFromRgb();
        }
        finally
        {
            this.updatingColour = false;
        }

        this.ValidateRgbInputs(requireValue: false);
    }

    private void ColourHex_LostFocus(object? sender, RoutedEventArgs e) => this.ApplyHexColour();

    private void ApplyHex_Click(object? sender, RoutedEventArgs e) => this.ApplyHexColour();

    private void ApplyHexColour()
    {
        var hexInput = this.ColourHex?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(hexInput))
        {
            this.ClearValidationError(ColourValidationKey);
            this.ClearColourInputs();
            return;
        }

        if (!GenerationInputService.TryParseHex(hexInput, out var colour))
        {
            this.SetValidationError(ColourValidationKey, "Hex colour must use the format #RRGGBB.", writeToLog: false);
            return;
        }

        this.SetColourInputs(colour);

        this.ClearValidationError(ColourValidationKey);
    }

    private void UpdateHexFromRgb()
    {
        if (this.TryGetCurrentColourSelection(out var colour))
        {
            this.ColourHex!.Text = colour.Hex;
        }
        else
        {
            this.ColourHex!.Text = string.Empty;
        }
    }

    private void UpdateColourPreview()
    {
        if (this.TryGetCurrentColourSelection(out var colour))
        {
            this.ColourPreview!.Background = new SolidColorBrush(Color.FromRgb(colour.R, colour.G, colour.B));
        }
        else
        {
            this.ColourPreview!.Background = Brushes.Transparent;
        }
    }

    private bool TryGetCurrentColourSelection(out ColourSelection colour)
    {
        return GenerationInputService.TryParseRgb(this.ColourR?.Text, this.ColourG?.Text, this.ColourB?.Text, out colour);
    }

    private void SetColourInputs(ColourSelection colour)
    {
        this.updatingColour = true;
        try
        {
            this.ColourR!.Text = colour.R.ToString();
            this.ColourG!.Text = colour.G.ToString();
            this.ColourB!.Text = colour.B.ToString();
            this.ColourHex!.Text = colour.Hex;
            this.ColourPreview!.Background = new SolidColorBrush(Color.FromRgb(colour.R, colour.G, colour.B));
        }
        finally
        {
            this.updatingColour = false;
        }
    }

    private async void GenerateButton_Click(object? sender, RoutedEventArgs e)
    {
        if (this.isGenerating)
        {
            return;
        }

        if (!this.TryBuildOptions(out var options))
        {
            return;
        }

        var shouldProceed = await this.ShowGenerationWarningAsync();
        if (!shouldProceed)
        {
            this.Log("Generation cancelled by user.");
            return;
        }

        this.SetGenerating(true);

        InstaFadeGenerator.GenerationResult result;
        try
        {
            result = await GenerationCoordinator.GenerateAsync(options, this.ReportProgress);
        }
        catch (Exception ex)
        {
            this.SetValidationError("Generation", $"Generation failed unexpectedly: {ex.Message}");
            this.SetGenerating(false);
            return;
        }

        this.ProgressBar!.Value = 100;
        if (result.Success)
        {
            this.ClearValidationError("Generation");
            this.Log(result.Message);
            this.Log("All done!");
        }
        else
        {
            this.SetValidationError("Generation", result.Message);
        }

        this.SetGenerating(false);
    }

    private Task<bool> ShowGenerationWarningAsync()
    {
        return new ConfirmGenerationWindow().ShowDialog<bool>(this);
    }

    private bool TryBuildOptions(out InstaFadeGenerator.GenerationOptions options)
    {
        options = default!;

        var skinFolderValidation = GenerationInputService.ValidateSkinFolder(this.SkinFolderTextBox?.Text, requireValue: true);
        this.ApplySkinFolderValidation(skinFolderValidation, clearWhenEmpty: false);
        if (!skinFolderValidation.IsValid)
        {
            return false;
        }

        var colourValidation = GenerationInputService.ValidateColourInput(
            this.ColourR?.Text,
            this.ColourG?.Text,
            this.ColourB?.Text,
            this.ColourHex?.Text,
            requireValue: true);
        this.ApplyColourValidation(colourValidation, clearWhenEmpty: false);
        if (!colourValidation.IsValid || colourValidation.Colour == null)
        {
            return false;
        }

        var skinFolder = skinFolderValidation.SkinFolderPath!;
        var colour = colourValidation.Colour.Value;

        options = new InstaFadeGenerator.GenerationOptions(
            skinFolder,
            colour.R,
            colour.G,
            colour.B,
            this.HdCheckBox?.IsChecked ?? false,
            this.BackupCheckBox?.IsChecked ?? false,
            this.TripleStackCheckBox?.IsChecked ?? false);
        return true;
    }

    private void ReportProgress(double progress, string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            this.ProgressBar!.Value = progress * 100;
            this.Log(message);
        });
    }

    private void SetGenerating(bool running)
    {
        this.isGenerating = running;
        this.BrowseButton!.IsEnabled = !running;
        this.SkinFolderTextBox!.IsEnabled = !running;
        this.ColourR!.IsEnabled = !running;
        this.ColourG!.IsEnabled = !running;
        this.ColourB!.IsEnabled = !running;
        this.ColourHex!.IsEnabled = !running;
        if (running)
        {
            this.ProgressBar!.Value = 0;
        }

        this.UpdateValidationUi();
    }

    private void Log(string message)
    {
        this.logState.Append(message);
        this.UpdateLogUi();
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

    private void ValidateRgbInputs(bool requireValue)
    {
        var validation = GenerationInputService.ValidateColourInput(
            this.ColourR?.Text,
            this.ColourG?.Text,
            this.ColourB?.Text,
            this.ColourHex?.Text,
            requireValue);

        this.ApplyColourValidation(validation, clearWhenEmpty: !requireValue);
    }

    private void ApplyColourValidation(ColourValidationResult validation, bool clearWhenEmpty)
    {
        if (validation.ErrorMessage != null)
        {
            this.SetValidationError(ColourValidationKey, validation.ErrorMessage, writeToLog: false);
        }
        else if (validation.HasValue || clearWhenEmpty)
        {
            this.ClearValidationError(ColourValidationKey);
        }
    }

    private void SetValidationError(string key, string message, bool writeToLog = true)
    {
        var changed = this.validationState.TrySet(key, message);
        if (changed && writeToLog)
        {
            this.Log($"ERROR: {message}");
        }

        this.UpdateValidationUi();
    }

    private void ClearValidationError(string key)
    {
        this.validationState.Clear(key);
        this.UpdateValidationUi();
    }

    private void UpdateValidationUi()
    {
        if (this.ErrorPanel != null)
        {
            this.ErrorPanel.IsVisible = this.validationState.HasErrors;
        }

        if (this.ErrorTextBlock != null)
        {
            this.ErrorTextBlock.Text = this.validationState.DisplayText;
        }

        if (this.GenerateButton != null)
        {
            this.GenerateButton.IsEnabled = !this.isGenerating && !this.validationState.HasErrors;
        }
    }

    private void UpdateLogUi()
    {
        if (this.LogTextBlock != null)
        {
            this.LogTextBlock.Text = this.logState.DisplayText;
        }
    }
}
