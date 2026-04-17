using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OsuInstaFadeSkinGenerator.Application.Ports;

namespace OsuInstaFadeSkinGenerator.Infrastructure.Avalonia;

public sealed class AvaloniaDialogService : IDialogService
{
    private readonly IDialogHost dialogHost;
    private readonly ILogger<AvaloniaDialogService> logger;

    public AvaloniaDialogService(IDialogHost dialogHost, ILogger<AvaloniaDialogService> logger)
    {
        this.dialogHost = dialogHost;
        this.logger = logger;
    }

    public async Task<bool> ConfirmGenerationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await this.dialogHost.ShowAsync<bool>(DialogKey.ConfirmGeneration, cancellationToken).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            this.logger.LogError(ex, "Failed to show the generation-confirmation dialog.");
            throw;
        }
    }
}
