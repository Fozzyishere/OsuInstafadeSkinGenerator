using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using OsuInstaFadeSkinGenerator.Application.Generation;
using OsuInstaFadeSkinGenerator.Infrastructure.Imaging;
using OsuInstaFadeSkinGenerator.Infrastructure.Io;
using OsuInstaFadeSkinGenerator.Infrastructure.SkinIni;
using OsuInstaFadeSkinGenerator.Services;
using OsuInstaFadeSkinGenerator.ViewModels;
using OsuInstaFadeSkinGenerator.Views;

namespace OsuInstaFadeSkinGenerator;

public partial class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var inputValidationService = new InputValidationService();
            var fileSystem = new PhysicalFileSystem();
            var skinIniReader = new SkinIniReader(fileSystem);
            var skinIniWriter = new SkinIniWriter(fileSystem);
            var imageIo = new ImageSharpImageIo();
            var windowInteractionService = new WindowInteractionService();
            var viewModel = new MainWindowViewModel(
                inputValidationService,
                skinIniReader,
                new InstaFadeGenerationOrchestrator(skinIniReader, skinIniWriter, fileSystem, imageIo),
                windowInteractionService);

            desktop.MainWindow = new MainWindow(viewModel, windowInteractionService);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
