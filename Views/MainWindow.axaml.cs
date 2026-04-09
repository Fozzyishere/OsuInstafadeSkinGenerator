using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
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

    private void SkinFolderTextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (this.DataContext is MainWindowViewModel viewModel
            && viewModel.ConfirmSkinFolderPathCommand.CanExecute(null))
        {
            viewModel.ConfirmSkinFolderPathCommand.Execute(null);
        }
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
