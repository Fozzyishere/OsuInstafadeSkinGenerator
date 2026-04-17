using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OsuInstaFadeSkinGenerator.Application.Ports;

namespace OsuInstaFadeSkinGenerator.Infrastructure.Avalonia;

public sealed class AvaloniaClipboardService : IClipboardService
{
    private readonly IOwnerWindowProvider ownerWindowProvider;
    private readonly ILogger<AvaloniaClipboardService> logger;

    public AvaloniaClipboardService(
        IOwnerWindowProvider ownerWindowProvider,
        ILogger<AvaloniaClipboardService> logger)
    {
        this.ownerWindowProvider = ownerWindowProvider;
        this.logger = logger;
    }

    public async Task SetClipboardTextAsync(string text, CancellationToken cancellationToken = default)
    {
        var owner = this.ownerWindowProvider.Current
            ?? throw new InvalidOperationException("No owner window is registered; clipboard is unavailable.");

        var clipboard = owner.Clipboard
            ?? throw new InvalidOperationException("Clipboard is not available on this system.");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await clipboard.SetTextAsync(text).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            this.logger.LogError(ex, "Failed to write text to the clipboard.");
            throw;
        }
    }
}
