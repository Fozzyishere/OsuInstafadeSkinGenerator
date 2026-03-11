using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OsuInstaFadeSkinGenerator.Views.Dialogs;

public partial class ConfirmGenerationWindow : Window
{
    public ConfirmGenerationWindow()
    {
        this.InitializeComponent();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        this.Close(false);
    }

    private void ProceedButton_Click(object? sender, RoutedEventArgs e)
    {
        this.Close(true);
    }
}
