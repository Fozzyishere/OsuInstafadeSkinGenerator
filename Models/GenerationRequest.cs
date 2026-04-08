namespace OsuInstaFadeSkinGenerator.Models;

public sealed record GenerationRequest(
    string SkinFolderPath,
    byte ComboR,
    byte ComboG,
    byte ComboB,
    bool ProcessHd,
    bool BackupFiles,
    bool EnableTripleStacking);
