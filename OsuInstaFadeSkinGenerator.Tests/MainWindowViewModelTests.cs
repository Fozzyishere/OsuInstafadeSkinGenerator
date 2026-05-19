using OsuInstaFadeSkinGenerator.Application.Generation;
using OsuInstaFadeSkinGenerator.Application.Ports;
using OsuInstaFadeSkinGenerator.Application.Validation;
using OsuInstaFadeSkinGenerator.Domain;
using OsuInstaFadeSkinGenerator.ViewModels;
using OsuInstaFadeSkinGenerator.ViewModels.Sections;

namespace OsuInstaFadeSkinGenerator.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task GenerateCommand_WhileRunning_BlocksCloseUntilCancelledOutcomeCompletes()
    {
        var generationService = new BlockingGenerationService();
        var viewModel = CreateReadyViewModel(generationService);

        var runTask = viewModel.GenerateCommand.ExecuteAsync(null);
        await generationService.Started.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(viewModel.IsCloseBlocked);
        Assert.True(viewModel.IsGenerating);
        Assert.False(viewModel.IsInputEnabled);
        Assert.False(viewModel.CanGenerate);
        Assert.True(viewModel.CanCancel);

        viewModel.NotifyCloseBlocked();
        Assert.Contains("Please wait", viewModel.Log.LogText, StringComparison.Ordinal);

        viewModel.CancelCommand.Execute(null);
        Assert.Equal(GenerationRunState.Cancelling, viewModel.GenerationRunState);
        Assert.False(viewModel.CanCancel);
        Assert.True(viewModel.IsCloseBlocked);

        generationService.AllowCompletion.SetResult();
        await runTask.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Equal(GenerationRunState.Idle, viewModel.GenerationRunState);
        Assert.False(viewModel.IsCloseBlocked);
        Assert.False(viewModel.IsGenerating);
        Assert.True(viewModel.IsInputEnabled);
        Assert.Contains("Generation cancelled.", viewModel.Log.LogText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateCommand_RollingBackProgress_UpdatesRunState()
    {
        var generationService = new ProgressReportingGenerationService();
        var viewModel = CreateReadyViewModel(generationService);

        await viewModel.GenerateCommand.ExecuteAsync(null);
        await WaitUntilAsync(
            () => viewModel.Log.LogText.Contains("Restoring original files", StringComparison.Ordinal),
            TimeSpan.FromSeconds(1));

        Assert.Equal(GenerationRunState.Idle, viewModel.GenerationRunState);
        Assert.Contains("Restoring original files", viewModel.Log.LogText, StringComparison.Ordinal);
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        while (!predicate())
        {
            await Task.Delay(10, cts.Token).ConfigureAwait(false);
        }
    }

    private static MainWindowViewModel CreateReadyViewModel(IGenerationService generationService)
    {
        var validation = new InputValidationService();
        var folder = new SkinFolderSectionViewModel(validation, new StubSkinIniReader(), new StubFilePickerService())
        {
            ActiveSkinFolderPath = "C:\\skins\\test",
        };
        var colour = new ColourSectionViewModel(validation);
        colour.ApplyFromComboColour(new RgbColor(1, 2, 3));

        return new MainWindowViewModel(
            validation,
            generationService,
            new ConfirmingDialogService(),
            folder,
            colour,
            new GenerationOptionsViewModel(),
            new GenerationLogViewModel(new StubClipboardService()));
    }

    private sealed class BlockingGenerationService : IGenerationService
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource AllowCompletion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<GenerationOutcome> GenerateAsync(
            GenerationRequest request,
            IProgress<GenerationProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            this.Started.SetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
                return new GenerationOutcome(GenerationStatus.Succeeded, null, "Unexpected completion.");
            }
            catch (OperationCanceledException)
            {
                await this.AllowCompletion.Task.ConfigureAwait(false);
                return new GenerationOutcome(GenerationStatus.Cancelled, null, "Generation cancelled.");
            }
        }
    }

    private sealed class ProgressReportingGenerationService : IGenerationService
    {
        public Task<GenerationOutcome> GenerateAsync(
            GenerationRequest request,
            IProgress<GenerationProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            progress?.Report(new GenerationProgress(
                GenerationPhase.RollingBack,
                0,
                "Restoring original files..."));
            return Task.FromResult(new GenerationOutcome(GenerationStatus.Cancelled, null, "Generation cancelled."));
        }
    }

    private sealed class ConfirmingDialogService : IDialogService
    {
        public Task<bool> ConfirmGenerationAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class StubSkinIniReader : ISkinIniReader
    {
        public Task<SkinConfig> ReadAsync(string skinIniPath, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class StubFilePickerService : IFilePickerService
    {
        public Task<string?> PickSkinFolderAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
    }

    private sealed class StubClipboardService : IClipboardService
    {
        public Task SetClipboardTextAsync(string text, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
