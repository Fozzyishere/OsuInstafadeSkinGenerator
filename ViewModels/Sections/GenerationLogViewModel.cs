using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsuInstaFadeSkinGenerator.Application.Ports;
using OsuInstaFadeSkinGenerator.Domain;

namespace OsuInstaFadeSkinGenerator.ViewModels.Sections;

public sealed partial class GenerationLogViewModel : ObservableObject
{
    private const string DefaultLogMessage = "Ready. Select a skin folder to get started.";

    private readonly IClipboardService clipboardService;
    private readonly List<string> logEntries = [];

    [ObservableProperty]
    private string logText = DefaultLogMessage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopyLogsCommand))]
    private bool hasLogEntries;

    [ObservableProperty]
    private double progressValue;

    public GenerationLogViewModel(IClipboardService clipboardService)
    {
        this.clipboardService = clipboardService;
        this.Entries = new ReadOnlyCollection<string>(this.logEntries);
    }

    public event EventHandler? CopySucceeded;

    public event EventHandler<string>? CopyFailed;

    public ReadOnlyCollection<string> Entries { get; }

    public void Append(string message)
    {
        this.logEntries.Add(message);
        this.RefreshLogText();
    }

    public void Reset()
    {
        this.logEntries.Clear();
        this.ProgressValue = 0;
        this.RefreshLogText();
    }

    public void ReportProgress(GenerationProgress progress)
    {
        this.ProgressValue = progress.Fraction * 100;
        this.Append(progress.Message);
    }

    [RelayCommand(CanExecute = nameof(HasLogEntries))]
    private async Task CopyLogsAsync()
    {
        if (!this.HasLogEntries)
        {
            return;
        }

        try
        {
            await this.clipboardService.SetClipboardTextAsync(this.LogText);
            this.CopySucceeded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            this.CopyFailed?.Invoke(this, $"Failed to copy logs: {ex.Message}");
        }
    }

    private void RefreshLogText()
    {
        this.HasLogEntries = this.logEntries.Count > 0;
        this.LogText = this.HasLogEntries
            ? string.Join(Environment.NewLine, this.logEntries)
            : DefaultLogMessage;
    }
}
