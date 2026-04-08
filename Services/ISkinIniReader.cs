using OsuInstaFadeSkinGenerator.Models;

namespace OsuInstaFadeSkinGenerator.Services;

public interface ISkinIniReader
{
    SkinConfig Read(string skinIniPath);
}
