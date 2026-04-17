using System.Collections.ObjectModel;
using OsuInstaFadeSkinGenerator.Domain;
using OsuInstaFadeSkinGenerator.Application.Validation;

namespace OsuInstaFadeSkinGenerator.Tests;

public sealed class InputValidationServiceTests
{
    private readonly InputValidationService sut = new();

    [Fact]
    public void ValidateSkinFolder_NullOrWhitespaceWithoutRequirement_ReturnsEmpty()
    {
        Assert.IsType<SkinFolderValidation.Empty>(this.sut.ValidateSkinFolder(null, requireValue: false));
        Assert.IsType<SkinFolderValidation.Empty>(this.sut.ValidateSkinFolder(string.Empty, requireValue: false));
        Assert.IsType<SkinFolderValidation.Empty>(this.sut.ValidateSkinFolder("   ", requireValue: false));
    }

    [Fact]
    public void ValidateSkinFolder_EmptyWithRequirement_ReturnsInvalid()
    {
        var result = this.sut.ValidateSkinFolder(string.Empty, requireValue: true);

        var invalid = Assert.IsType<SkinFolderValidation.Invalid>(result);
        Assert.Contains("valid osu! skin folder", invalid.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateSkinFolder_NonExistentPath_ReturnsInvalid()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var result = this.sut.ValidateSkinFolder(nonExistentPath, requireValue: true);

        var invalid = Assert.IsType<SkinFolderValidation.Invalid>(result);
        Assert.Contains("does not exist", invalid.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateSkinFolder_ExistingFolderWithoutSkinIni_ReturnsInvalid()
    {
        using var skinDir = new TestSkinDirectory();

        var result = this.sut.ValidateSkinFolder(skinDir.RootPath, requireValue: true);

        var invalid = Assert.IsType<SkinFolderValidation.Invalid>(result);
        Assert.Contains("skin.ini", invalid.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateSkinFolder_ExistingFolderWithSkinIni_ReturnsValid()
    {
        using var skinDir = new TestSkinDirectory();
        SkinIniTemplateFixture.WriteTemplateSkinIni(skinDir.RootPath, 1);

        var result = this.sut.ValidateSkinFolder(skinDir.RootPath, requireValue: true);

        var valid = Assert.IsType<SkinFolderValidation.Valid>(result);
        Assert.Equal(Path.GetFullPath(skinDir.RootPath), valid.SkinFolderPath);
        Assert.Equal(Path.Combine(Path.GetFullPath(skinDir.RootPath), SkinAssetNames.SkinIni), valid.SkinIniPath);
    }

    [Fact]
    public void ValidateSkinFolder_PathWithInvalidChars_ReturnsInvalid()
    {
        var pathWithNullChar = "C:\\invalid\0path";

        var result = this.sut.ValidateSkinFolder(pathWithNullChar, requireValue: true);

        Assert.IsType<SkinFolderValidation.Invalid>(result);
    }

    [Fact]
    public void ValidateRgbInput_AllNullOrEmptyWithoutRequirement_ReturnsEmpty()
    {
        Assert.IsType<ColourValidation.Empty>(this.sut.ValidateRgbInput(null, null, null, requireValue: false));
        Assert.IsType<ColourValidation.Empty>(this.sut.ValidateRgbInput(string.Empty, string.Empty, string.Empty, requireValue: false));
        Assert.IsType<ColourValidation.Empty>(this.sut.ValidateRgbInput("   ", "   ", "   ", requireValue: false));
    }

    [Fact]
    public void ValidateRgbInput_AllEmptyWithRequirement_ReturnsInvalid()
    {
        var result = this.sut.ValidateRgbInput(null, null, null, requireValue: true);

        Assert.IsType<ColourValidation.Invalid>(result);
    }

    [Theory]
    [InlineData("10", "20", "30", 10, 20, 30)]
    [InlineData("255", "0", "128", 255, 0, 128)]
    public void ValidateRgbInput_ValidTriplet_ReturnsValid(string r, string g, string b, byte expectedR, byte expectedG, byte expectedB)
    {
        var result = this.sut.ValidateRgbInput(r, g, b, requireValue: true);

        var valid = Assert.IsType<ColourValidation.Valid>(result);
        Assert.Equal(new RgbColor(expectedR, expectedG, expectedB), valid.Color);
    }

    [Theory]
    [InlineData("256", "0", "0")]
    [InlineData("abc", "0", "0")]
    [InlineData("10", "20", null)]
    public void ValidateRgbInput_PartialOrOutOfRange_ReturnsInvalid(string? r, string? g, string? b)
    {
        var result = this.sut.ValidateRgbInput(r, g, b, requireValue: true);

        Assert.IsType<ColourValidation.Invalid>(result);
    }

    [Fact]
    public void ValidateHexInput_EmptyWithoutRequirement_ReturnsEmpty()
    {
        Assert.IsType<ColourValidation.Empty>(this.sut.ValidateHexInput(null, requireValue: false));
        Assert.IsType<ColourValidation.Empty>(this.sut.ValidateHexInput(string.Empty, requireValue: false));
        Assert.IsType<ColourValidation.Empty>(this.sut.ValidateHexInput("   ", requireValue: false));
    }

    [Fact]
    public void ValidateHexInput_EmptyWithRequirement_ReturnsInvalid()
    {
        var result = this.sut.ValidateHexInput(null, requireValue: true);

        Assert.IsType<ColourValidation.Invalid>(result);
    }

    [Theory]
    [InlineData("#FFFFFF", 255, 255, 255)]
    [InlineData("FFFFFF", 255, 255, 255)]
    [InlineData("#f1d6cf", 241, 214, 207)]
    public void ValidateHexInput_ValidHex_ReturnsValid(string input, byte r, byte g, byte b)
    {
        var result = this.sut.ValidateHexInput(input, requireValue: true);

        var valid = Assert.IsType<ColourValidation.Valid>(result);
        Assert.Equal(new RgbColor(r, g, b), valid.Color);
    }

    [Theory]
    [InlineData("#FFF")]
    [InlineData("#GGGGGG")]
    [InlineData("not-a-color")]
    public void ValidateHexInput_InvalidHex_ReturnsInvalid(string input)
    {
        var result = this.sut.ValidateHexInput(input, requireValue: true);

        Assert.IsType<ColourValidation.Invalid>(result);
    }

    [Fact]
    public void GetPrimaryComboColour_EmptyComboList_ReturnsNull()
    {
        var config = CreateConfig([]);

        var result = this.sut.GetPrimaryComboColour(config);

        Assert.Null(result);
    }

    [Fact]
    public void GetPrimaryComboColour_MultipleCombos_ReturnsLowestIndex()
    {
        var config = CreateConfig([
            (3, new RgbColor(30, 30, 30)),
            (1, new RgbColor(10, 10, 10)),
            (2, new RgbColor(20, 20, 20)),
        ]);

        var result = this.sut.GetPrimaryComboColour(config);

        Assert.Equal(new RgbColor(10, 10, 10), result);
    }

    private static SkinConfig CreateConfig(IReadOnlyList<(int Index, RgbColor Color)> combos)
    {
        return new SkinConfig(
            Name: "Test",
            Author: "Test",
            Version: "1.0",
            HitCircleOverlayAboveNumber: true,
            ComboColours: new ReadOnlyCollection<(int, RgbColor)>(combos.ToList()),
            SliderBorder: null,
            SliderTrackOverride: null,
            HitCirclePrefix: "default",
            HitCircleOverlap: -2);
    }
}
