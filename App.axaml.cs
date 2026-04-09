using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using OsuInstaFadeSkinGenerator.Services;
using OsuInstaFadeSkinGenerator.ViewModels;
using OsuInstaFadeSkinGenerator.Views;

namespace OsuInstaFadeSkinGenerator;

public partial class App : Application
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
            var skinIniReader = new SkinIniReader();
            var skinIniWriter = new SkinIniWriter();
            var windowInteractionService = new WindowInteractionService();
            var viewModel = new MainWindowViewModel(
                inputValidationService,
                skinIniReader,
                new InstaFadeGenerator(skinIniReader, skinIniWriter),
                windowInteractionService);

            desktop.MainWindow = new MainWindow(viewModel, windowInteractionService);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
