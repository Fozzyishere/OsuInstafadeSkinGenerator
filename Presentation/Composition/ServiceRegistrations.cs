using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OsuInstaFadeSkinGenerator.Application.Generation;
using OsuInstaFadeSkinGenerator.Application.Ports;
using OsuInstaFadeSkinGenerator.Infrastructure.Avalonia;
using OsuInstaFadeSkinGenerator.Infrastructure.Imaging;
using OsuInstaFadeSkinGenerator.Infrastructure.Io;
using OsuInstaFadeSkinGenerator.Infrastructure.SkinIni;
using OsuInstaFadeSkinGenerator.Presentation.Dialogs;
using OsuInstaFadeSkinGenerator.Services;
using OsuInstaFadeSkinGenerator.ViewModels;
using OsuInstaFadeSkinGenerator.ViewModels.Sections;
using OsuInstaFadeSkinGenerator.Views;

namespace OsuInstaFadeSkinGenerator.Presentation.Composition;

public static class ServiceRegistrations
{
    public static IServiceCollection AddGeneratorApp(this IServiceCollection services)
    {
        services.AddLogging(builder => builder.AddDebug());

        services.AddSingleton<IFileSystem, PhysicalFileSystem>();
        services.AddSingleton<ISkinIniReader, SkinIniReader>();
        services.AddSingleton<ISkinIniWriter, SkinIniWriter>();
        services.AddSingleton<IImageIo, ImageSharpImageIo>();
        services.AddSingleton<IInputValidationService, InputValidationService>();
        services.AddSingleton<IGenerationService, InstaFadeGenerationOrchestrator>();

        services.AddSingleton<OwnerWindowProvider>();
        services.AddSingleton<IOwnerWindowProvider>(sp => sp.GetRequiredService<OwnerWindowProvider>());
        services.AddSingleton<IDialogHost, AvaloniaDialogHost>();

        services.AddSingleton<IFilePickerService, AvaloniaFilePickerService>();
        services.AddSingleton<IDialogService, AvaloniaDialogService>();
        services.AddSingleton<IClipboardService, AvaloniaClipboardService>();

        services.AddTransient<SkinFolderSectionViewModel>();
        services.AddTransient<ColourSectionViewModel>();
        services.AddTransient<GenerationOptionsViewModel>();
        services.AddTransient<GenerationLogViewModel>();
        services.AddTransient<MainWindowViewModel>();

        services.AddTransient<MainWindow>();

        return services;
    }
}
