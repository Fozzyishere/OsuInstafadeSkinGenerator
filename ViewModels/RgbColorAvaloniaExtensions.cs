using Avalonia.Media;
using OsuInstaFadeSkinGenerator.Domain;

namespace OsuInstaFadeSkinGenerator.ViewModels;

internal static class RgbColorAvaloniaExtensions
{
    public static Color ToAvaloniaColor(this RgbColor color) =>
        Color.FromRgb(color.R, color.G, color.B);
}
