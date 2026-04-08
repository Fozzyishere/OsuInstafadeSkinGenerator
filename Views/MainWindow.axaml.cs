using Avalonia.Controls;
using OsuInstaFadeSkinGenerator.Services;
using OsuInstaFadeSkinGenerator.ViewModels;

namespace OsuInstaFadeSkinGenerator.Views;

public partial class MainWindow : Window
{
    public MainWindow()
        : this(MainWindowViewModel.CreateDesignTime(), new WindowInteractionService())
    {
    }

    public MainWindow(MainWindowViewModel viewModel, WindowInteractionService interactionService)
    {
        this.InitializeComponent();
        this.DataContext = viewModel;
        interactionService.Attach(this);
    }
}
