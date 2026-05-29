using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OsuInstaFadeSkinGenerator.Views.Sections;

public partial class LogSectionView : UserControl
{
    public LogSectionView()
    {
        this.InitializeComponent();
    }

    private void LogOutput_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox logOutput)
        {
            return;
        }

        var lineCount = logOutput.GetLineCount();
        if (lineCount <= 0)
        {
            return;
        }

        logOutput.ScrollToLine(lineCount - 1);
    }
}
