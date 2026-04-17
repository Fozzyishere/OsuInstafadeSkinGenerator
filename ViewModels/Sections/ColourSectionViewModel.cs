using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsuInstaFadeSkinGenerator.Models;
using OsuInstaFadeSkinGenerator.Services;

namespace OsuInstaFadeSkinGenerator.ViewModels.Sections;

public sealed partial class ColourSectionViewModel : ObservableObject
{
    private readonly IInputValidationService inputValidationService;
    private bool isUpdatingColour;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPendingConfirmation))]
    private string colourRText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPendingConfirmation))]
    private string colourGText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPendingConfirmation))]
    private string colourBText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPendingConfirmation))]
    private string colourHex = string.Empty;

    [ObservableProperty]
    private IBrush colourPreviewBrush = Brushes.Transparent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPendingConfirmation))]
    private RgbColor? appliedColour;

    public ColourSectionViewModel(IInputValidationService inputValidationService)
    {
        this.inputValidationService = inputValidationService;
    }

    public event EventHandler<string?>? ErrorChanged;

    public bool HasPendingConfirmation =>
        this.AppliedColour == null
            ? !string.IsNullOrWhiteSpace(this.ColourRText)
                || !string.IsNullOrWhiteSpace(this.ColourGText)
                || !string.IsNullOrWhiteSpace(this.ColourBText)
                || !string.IsNullOrWhiteSpace(this.ColourHex)
            : !string.Equals(this.ColourRText, this.AppliedColour.Value.R.ToString(), StringComparison.Ordinal)
                || !string.Equals(this.ColourGText, this.AppliedColour.Value.G.ToString(), StringComparison.Ordinal)
                || !string.Equals(this.ColourBText, this.AppliedColour.Value.B.ToString(), StringComparison.Ordinal)
                || !string.Equals(this.ColourHex, this.AppliedColour.Value.Hex, StringComparison.Ordinal);

    public void ApplyFromComboColour(RgbColor? comboColour)
    {
        if (comboColour == null)
        {
            this.ClearColourInputs();
            return;
        }

        this.CommitAppliedColour(comboColour.Value);
    }

    public void ClearColourInputs()
    {
        this.AppliedColour = null;
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

        this.RaiseErrorChanged(null);
    }

    public void ReportMissingComboColour(string message)
    {
        this.RaiseErrorChanged(message);
    }

    partial void OnColourRTextChanged(string value) => this.OnColourDraftChanged();

    partial void OnColourGTextChanged(string value) => this.OnColourDraftChanged();

    partial void OnColourBTextChanged(string value) => this.OnColourDraftChanged();

    partial void OnColourHexChanged(string value) => this.OnColourDraftChanged();

    [RelayCommand]
    private void ApplyRgb()
    {
        switch (this.inputValidationService.ValidateRgbInput(
            this.ColourRText,
            this.ColourGText,
            this.ColourBText,
            requireValue: false))
        {
            case ColourValidation.Invalid invalid:
                this.RaiseErrorChanged(invalid.Message);
                break;
            case ColourValidation.Empty:
                this.ClearColourInputs();
                break;
            case ColourValidation.Valid valid:
                this.CommitAppliedColour(valid.Color);
                break;
        }
    }

    [RelayCommand]
    private void ApplyHex()
    {
        switch (this.inputValidationService.ValidateHexInput(this.ColourHex, requireValue: false))
        {
            case ColourValidation.Invalid invalid:
                this.RaiseErrorChanged(invalid.Message);
                break;
            case ColourValidation.Empty:
                this.ClearColourInputs();
                break;
            case ColourValidation.Valid valid:
                this.CommitAppliedColour(valid.Color);
                break;
        }
    }

    private void OnColourDraftChanged()
    {
        if (this.isUpdatingColour)
        {
            return;
        }

        this.RaiseErrorChanged(null);
    }

    private void CommitAppliedColour(RgbColor colour)
    {
        this.AppliedColour = colour;
        this.SetColourInputs(colour);
        this.RaiseErrorChanged(null);
    }

    private void SetColourInputs(RgbColor colour)
    {
        this.isUpdatingColour = true;
        try
        {
            this.ColourRText = colour.R.ToString();
            this.ColourGText = colour.G.ToString();
            this.ColourBText = colour.B.ToString();
            this.ColourHex = colour.Hex;
            this.ColourPreviewBrush = new SolidColorBrush(colour.ToAvaloniaColor());
        }
        finally
        {
            this.isUpdatingColour = false;
        }
    }

    private void RaiseErrorChanged(string? message)
    {
        this.ErrorChanged?.Invoke(this, message);
    }
}
