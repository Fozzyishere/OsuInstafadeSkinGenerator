namespace OsuInstaFadeSkinGenerator.Domain;

public sealed record GenerationRequest(
    string SkinFolderPath,
    RgbColor ComboColor,
    bool ProcessHd,
    bool BackupFiles,
    bool EnableTripleStacking);
