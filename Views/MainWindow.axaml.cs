using Avalonia.Controls;
using OsuInstaFadeSkinGenerator.Services;
using OsuInstaFadeSkinGenerator.ViewModels;

namespace OsuInstaFadeSkinGenerator.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        var interactionService = new WindowInteractionService();
        this.Initialize(interactionService, MainWindowViewModel.CreateDesignTime(interactionService));
    }

    public MainWindow(MainWindowViewModel viewModel, WindowInteractionService interactionService)
    {
        this.Initialize(interactionService, viewModel);
    }

    private void Initialize(WindowInteractionService interactionService, MainWindowViewModel viewModel)
    {
        this.InitializeComponent();
        this.DataContext = viewModel;
        interactionService.Attach(this);
    }
}
