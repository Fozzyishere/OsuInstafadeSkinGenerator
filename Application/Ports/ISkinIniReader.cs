using System.Threading;
using System.Threading.Tasks;
using OsuInstaFadeSkinGenerator.Domain;

namespace OsuInstaFadeSkinGenerator.Application.Ports;

public interface ISkinIniReader
{
    Task<SkinConfig> ReadAsync(string skinIniPath, CancellationToken cancellationToken);
}
