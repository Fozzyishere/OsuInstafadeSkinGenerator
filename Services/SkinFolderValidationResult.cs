namespace OsuInstaFadeSkinGenerator.Services;

public sealed record SkinFolderValidationResult(string? SkinFolderPath, string? SkinIniPath, string? ErrorMessage)
{
    public bool IsValid => this.ErrorMessage == null && this.SkinFolderPath != null && this.SkinIniPath != null;

    public bool HasValue => this.SkinFolderPath != null || this.SkinIniPath != null;

    public static SkinFolderValidationResult Empty => new(null, null, null);

    public static SkinFolderValidationResult Invalid(string errorMessage) => new(null, null, errorMessage);

    public static SkinFolderValidationResult Valid(string skinFolderPath, string skinIniPath) => new(skinFolderPath, skinIniPath, null);
}
