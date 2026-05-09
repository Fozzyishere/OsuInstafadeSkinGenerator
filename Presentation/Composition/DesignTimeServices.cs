using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using OsuInstaFadeSkinGenerator.Application.Generation;
using OsuInstaFadeSkinGenerator.Application.Ports;
using OsuInstaFadeSkinGenerator.Application.Validation;
using OsuInstaFadeSkinGenerator.Infrastructure.Imaging;
using OsuInstaFadeSkinGenerator.Infrastructure.Io;
using OsuInstaFadeSkinGenerator.Infrastructure.SkinIni;
using OsuInstaFadeSkinGenerator.ViewModels;
using OsuInstaFadeSkinGenerator.ViewModels.Sections;

namespace OsuInstaFadeSkinGenerator.Presentation.Composition;

public static class DesignTimeServices
{
    public static MainWindowViewModel CreateMainWindowViewModel()
    {
        var inputValidationService = new InputValidationService();
        var fileSystem = new PhysicalFileSystem();
        var skinIniReader = new SkinIniReader(fileSystem);
        var skinIniWriter = new SkinIniWriter(fileSystem);
        var imageIo = new ImageSharpImageIo(fileSystem);
        var orchestrator = new InstaFadeGenerationOrchestrator(
            skinIniReader,
            skinIniWriter,
            fileSystem,
            imageIo,
            NullLogger<InstaFadeGenerationOrchestrator>.Instance);

        var filePicker = new StubFilePickerService();
        var dialog = new StubDialogService();
        var clipboard = new StubClipboardService();

        return new MainWindowViewModel(
            inputValidationService,
            orchestrator,
            dialog,
            new SkinFolderSectionViewModel(inputValidationService, skinIniReader, filePicker),
            new ColourSectionViewModel(inputValidationService),
            new GenerationOptionsViewModel(),
            new GenerationLogViewModel(clipboard));
    }

    private sealed class StubFilePickerService : IFilePickerService
    {
        public Task<string?> PickSkinFolderAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);
    }

    private sealed class StubDialogService : IDialogService
    {
        public Task<bool> ConfirmGenerationAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class StubClipboardService : IClipboardService
    {
        public Task SetClipboardTextAsync(string text, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
