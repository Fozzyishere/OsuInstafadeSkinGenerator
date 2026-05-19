namespace OsuInstaFadeSkinGenerator.Domain;

public enum GenerationError
{
    SkinFolderMissing,
    SkinIniMissing,
    MissingHdAsset,
    IoFailure,
    ImageDecodeFailure,
    RollbackFailed,
    Unexpected,
}
