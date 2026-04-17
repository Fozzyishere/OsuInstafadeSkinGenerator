using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OsuInstaFadeSkinGenerator.Presentation.Composition;
using OsuInstaFadeSkinGenerator.ViewModels;
using OsuInstaFadeSkinGenerator.Views;

namespace OsuInstaFadeSkinGenerator;

public partial class App : Avalonia.Application
{
    private IHost? host;

    public IServiceProvider? Services => this.host?.Services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddGeneratorApp();
            this.host = builder.Build();

            var mainWindow = this.host.Services.GetRequiredService<MainWindow>();
            mainWindow.DataContext = this.host.Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = mainWindow;
            desktop.Exit += (_, _) =>
            {
                this.host?.Dispose();
                this.host = null;
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
