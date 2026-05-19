using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using OsuInstaFadeSkinGenerator.Presentation.Composition;
using OsuInstaFadeSkinGenerator.Presentation.Dialogs;
using OsuInstaFadeSkinGenerator.ViewModels;

namespace OsuInstaFadeSkinGenerator.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();

        if (Design.IsDesignMode)
        {
            this.DataContext = DesignTimeServices.CreateMainWindowViewModel();
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (Design.IsDesignMode)
        {
            return;
        }

        if (Avalonia.Application.Current is App app && app.Services is { } services)
        {
            services.GetRequiredService<OwnerWindowProvider>().Register(this);
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (this.DataContext is MainWindowViewModel { IsCloseBlocked: true } viewModel)
        {
            e.Cancel = true;
            viewModel.NotifyCloseBlocked();
            return;
        }

        base.OnClosing(e);
    }

    private void Window_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Control control
            && control.GetSelfAndVisualAncestors().OfType<TextBox>().Any())
        {
            return;
        }

        this.FocusManager?.ClearFocus();
    }
}
