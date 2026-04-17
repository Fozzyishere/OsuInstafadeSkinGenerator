using System.Threading;
using System.Threading.Tasks;
using OsuInstaFadeSkinGenerator.Models;

namespace OsuInstaFadeSkinGenerator.Application.Ports;

public interface ISkinIniWriter
{
    Task UpdateAsync(string skinIniPath, RgbColor comboColor, int hitCircleOverlap, CancellationToken cancellationToken);
}
