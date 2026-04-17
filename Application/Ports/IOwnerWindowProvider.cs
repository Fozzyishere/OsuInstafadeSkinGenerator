using Avalonia.Controls;

namespace OsuInstaFadeSkinGenerator.Application.Ports;

public interface IOwnerWindowProvider
{
    Window? Current { get; }
}
