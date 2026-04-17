using System.Threading;
using System.Threading.Tasks;

namespace OsuInstaFadeSkinGenerator.Application.Ports;

public interface IFilePickerService
{
    Task<string?> PickSkinFolderAsync(CancellationToken cancellationToken = default);
}
