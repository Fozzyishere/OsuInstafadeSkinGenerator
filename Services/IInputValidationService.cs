using OsuInstaFadeSkinGenerator.Models;

namespace OsuInstaFadeSkinGenerator.Services;

public interface IInputValidationService
{
    RgbColor? GetPrimaryComboColour(SkinConfig config);

    SkinFolderValidation ValidateSkinFolder(string? inputPath, bool requireValue);

    ColourValidation ValidateRgbInput(
        string? redText,
        string? greenText,
        string? blueText,
        bool requireValue);

    ColourValidation ValidateHexInput(string? hexText, bool requireValue);

    bool TryParseRgb(string? redText, string? greenText, string? blueText, out RgbColor colour);

    bool TryParseHex(string? input, out RgbColor colour);
}
