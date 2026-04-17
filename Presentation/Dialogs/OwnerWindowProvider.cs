using Avalonia.Controls;
using OsuInstaFadeSkinGenerator.Application.Ports;

namespace OsuInstaFadeSkinGenerator.Presentation.Dialogs;

public sealed class OwnerWindowProvider : IOwnerWindowProvider
{
    public Window? Current { get; private set; }

    public void Register(Window window)
    {
        this.Current = window;
    }
}
