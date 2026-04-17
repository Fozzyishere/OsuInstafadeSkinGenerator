using System;

namespace OsuInstaFadeSkinGenerator.Domain;

public static class SkinAssetNames
{
    public const string SkinIni = "skin.ini";
    public const string BackupFolder = "_insta-fade-backup";
    public const string Hitcircle = "hitcircle.png";
    public const string HitcircleOverlay = "hitcircleoverlay.png";
    public const string HdSuffix = "@2x";

    public static string WithHd(string baseName)
        => baseName.Replace(".png", $"{HdSuffix}.png", StringComparison.Ordinal);
}
