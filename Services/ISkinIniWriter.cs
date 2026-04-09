namespace OsuInstaFadeSkinGenerator.Services;

public interface ISkinIniWriter
{
    void Update(string skinIniPath, byte comboR, byte comboG, byte comboB, int hitCircleOverlap);
}
