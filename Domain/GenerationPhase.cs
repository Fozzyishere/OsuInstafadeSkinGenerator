namespace OsuInstaFadeSkinGenerator.Domain;

public enum GenerationPhase
{
    ReadingIni,
    CreatingBackup,
    ProcessingSd,
    ProcessingHd,
    UpdatingIni,
    Done,
}
