using System.Threading;
using System.Threading.Tasks;

namespace OsuInstaFadeSkinGenerator.Application.Ports;

public interface IDialogService
{
    Task<bool> ConfirmGenerationAsync(CancellationToken cancellationToken = default);
}
