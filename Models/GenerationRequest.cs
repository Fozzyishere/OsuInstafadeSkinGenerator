namespace OsuInstaFadeSkinGenerator.Models;

public sealed record GenerationRequest(
    string SkinFolderPath,
    RgbColor ComboColor,
    bool ProcessHd,
    bool BackupFiles,
    bool EnableTripleStacking);
