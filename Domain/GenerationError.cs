namespace OsuInstaFadeSkinGenerator.Domain;

public enum GenerationError
{
    SkinFolderMissing,
    SkinIniMissing,
    MissingHdAsset,
    IoFailure,
    ImageDecodeFailure,
    Unexpected,
}
