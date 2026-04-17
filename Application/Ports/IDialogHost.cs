using System.Threading;
using System.Threading.Tasks;

namespace OsuInstaFadeSkinGenerator.Application.Ports;

public enum DialogKey
{
    ConfirmGeneration,
}

public interface IDialogHost
{
    Task<TResult?> ShowAsync<TResult>(DialogKey key, CancellationToken cancellationToken = default);
}
