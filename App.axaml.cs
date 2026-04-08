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
            var windowInteractionService = new WindowInteractionService();
            var viewModel = new MainWindowViewModel(
                new InputValidationService(),
                new SkinIniReader(),
                new InstaFadeGenerator(new SkinIniReader(), new SkinIniWriter()),
                windowInteractionService);

            desktop.MainWindow = new MainWindow(viewModel, windowInteractionService);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
