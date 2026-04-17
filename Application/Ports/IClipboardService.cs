using System.Threading;
using System.Threading.Tasks;

namespace OsuInstaFadeSkinGenerator.Application.Ports;

public interface IClipboardService
{
    Task SetClipboardTextAsync(string text, CancellationToken cancellationToken = default);
}
