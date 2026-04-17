using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using OsuInstaFadeSkinGenerator.Application.Ports;
using OsuInstaFadeSkinGenerator.Views.Dialogs;

namespace OsuInstaFadeSkinGenerator.Presentation.Dialogs;

public sealed class AvaloniaDialogHost : IDialogHost
{
    private readonly IOwnerWindowProvider ownerWindowProvider;
    private readonly Dictionary<DialogKey, Func<Window>> dialogFactories;

    public AvaloniaDialogHost(IOwnerWindowProvider ownerWindowProvider)
    {
        this.ownerWindowProvider = ownerWindowProvider;
        this.dialogFactories = new Dictionary<DialogKey, Func<Window>>
        {
            [DialogKey.ConfirmGeneration] = () => new ConfirmGenerationWindow(),
        };
    }

    public async Task<TResult?> ShowAsync<TResult>(DialogKey key, CancellationToken cancellationToken = default)
    {
        if (!this.dialogFactories.TryGetValue(key, out var factory))
        {
            throw new InvalidOperationException($"No dialog is registered for key '{key}'.");
        }

        var owner = this.ownerWindowProvider.Current
            ?? throw new InvalidOperationException("No owner window is registered; cannot show a dialog.");

        var dialog = factory();
        return await dialog.ShowDialog<TResult?>(owner).ConfigureAwait(true);
    }
}
