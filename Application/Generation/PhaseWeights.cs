namespace OsuInstaFadeSkinGenerator.Application.Generation;

internal static class PhaseWeights
{
    public const double ReadingIniStart = 0.0;
    public const double ReadingIniSkinMeta = 0.02;
    public const double ReadingIniPrefix = 0.03;
    public const double ReadingIniOverlay = 0.04;

    public const double BackupStart = 0.05;

    public const double SdStart = 0.10;
    public const double SdEnd = 0.50;

    public const double HdStart = 0.50;
    public const double HdEnd = 0.90;

    public const double UpdatingIniStart = 0.90;
    public const double Done = 1.0;

    public const double VariantUpscale = 0.10;
    public const double VariantTint = 0.20;
    public const double VariantComposite = 0.30;
    public const double VariantNumberBase = 0.30;
    public const double VariantNumberStep = 0.05;
    public const double VariantBlank = 0.85;
    public const double VariantReplace = 0.95;
}
