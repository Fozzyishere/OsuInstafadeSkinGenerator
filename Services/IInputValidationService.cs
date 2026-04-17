using OsuInstaFadeSkinGenerator.Models;

namespace OsuInstaFadeSkinGenerator.Services;

public interface IInputValidationService
{
    RgbColor? GetPrimaryComboColour(SkinConfig config);

    SkinFolderValidationResult ValidateSkinFolder(string? inputPath, bool requireValue);

    ColourValidationResult ValidateColourInput(
        string? redText,
        string? greenText,
        string? blueText,
        string? hexText,
        bool requireValue);

    ColourValidationResult ValidateRgbInput(
        string? redText,
        string? greenText,
        string? blueText,
        bool requireValue);

    ColourValidationResult ValidateHexInput(string? hexText, bool requireValue);

    bool TryParseRgb(string? redText, string? greenText, string? blueText, out RgbColor colour);

    bool TryParseHex(string? input, out RgbColor colour);
}
