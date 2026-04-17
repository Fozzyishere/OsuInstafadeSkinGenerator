using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OsuInstaFadeSkinGenerator.Views.Behaviors;

public static class LostFocusCommitBehavior
{
    public static readonly AttachedProperty<ICommand?> CommandProperty =
        AvaloniaProperty.RegisterAttached<Control, ICommand?>(
            "Command",
            typeof(LostFocusCommitBehavior));

    static LostFocusCommitBehavior()
    {
        CommandProperty.Changed.AddClassHandler<Control>(OnCommandChanged);
    }

    public static void SetCommand(Control element, ICommand? value) =>
        element.SetValue(CommandProperty, value);

    public static ICommand? GetCommand(Control element) =>
        element.GetValue(CommandProperty);

    private static void OnCommandChanged(Control control, AvaloniaPropertyChangedEventArgs args)
    {
        control.LostFocus -= OnLostFocus;
        if (args.NewValue is ICommand)
        {
            control.LostFocus += OnLostFocus;
        }
    }

    private static void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control control)
        {
            return;
        }

        var command = GetCommand(control);
        if (command is not null && command.CanExecute(null))
        {
            command.Execute(null);
        }
    }
}
