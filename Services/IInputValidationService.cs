using OsuInstaFadeSkinGenerator.Models;

namespace OsuInstaFadeSkinGenerator.Services;

public interface IInputValidationService
{
    ColourSelection? GetPrimaryComboColour(SkinConfig config);

    SkinFolderValidationResult ValidateSkinFolder(string? inputPath, bool requireValue);

    ColourValidationResult ValidateColourInput(
        string? redText,
        string? greenText,
        string? blueText,
        string? hexText,
        bool requireValue);

    bool TryParseRgb(string? redText, string? greenText, string? blueText, out ColourSelection colour);

    bool TryParseHex(string? input, out ColourSelection colour);
}
