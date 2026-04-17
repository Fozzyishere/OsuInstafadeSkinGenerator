namespace OsuInstaFadeSkinGenerator.Models;

public abstract record SkinFolderValidation
{
    private SkinFolderValidation()
    {
    }

    public sealed record Empty : SkinFolderValidation;

    public sealed record Invalid(string Message) : SkinFolderValidation;

    public sealed record Valid(string SkinFolderPath, string SkinIniPath) : SkinFolderValidation;
}
