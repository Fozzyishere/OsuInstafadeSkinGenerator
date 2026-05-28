namespace OsuInstaFadeSkinGenerator.Domain;

public enum GenerationError
{
    SkinFolderMissing,
    SkinIniMissing,
    MissingHdAsset,
    IoFailure,
    UnsafeOutputPath,
    ImageDecodeFailure,
    RollbackFailed,
    Unexpected,
}
