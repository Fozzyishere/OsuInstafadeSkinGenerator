namespace OsuInstaFadeSkinGenerator.Models;

public enum GenerationError
{
    SkinFolderMissing,
    SkinIniMissing,
    MissingHdAsset,
    IoFailure,
    ImageDecodeFailure,
    Unexpected,
}
