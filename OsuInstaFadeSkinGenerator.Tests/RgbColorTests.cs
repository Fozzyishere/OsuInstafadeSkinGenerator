using System.Globalization;
using OsuInstaFadeSkinGenerator.Domain;

namespace OsuInstaFadeSkinGenerator.Tests;

public sealed class RgbColorTests
{
    [Theory]
    [InlineData("#FFFFFF", 255, 255, 255)]
    [InlineData("FFFFFF", 255, 255, 255)]
    [InlineData("#000000", 0, 0, 0)]
    [InlineData("#00FF10", 0, 255, 16)]
    [InlineData("  #00FF10  ", 0, 255, 16)]
    [InlineData("#f1d6cf", 241, 214, 207)]
    public void TryParseHex_ValidInputs_ReturnsColor(string input, byte r, byte g, byte b)
    {
        var parsed = RgbColor.TryParseHex(input, out var color);

        Assert.True(parsed);
        Assert.Equal(new RgbColor(r, g, b), color);
    }

    [Theory]
    [InlineData("#FFF")]
    [InlineData("#GGGGGG")]
    [InlineData("#GG0000")]
    [InlineData("#FFFFFFF")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-color")]
    public void TryParseHex_InvalidInputs_ReturnsFalse(string? input)
    {
        var parsed = RgbColor.TryParseHex(input, out var color);

        Assert.False(parsed);
        Assert.Equal(default, color);
    }

    [Theory]
    [InlineData("0", "0", "0", 0, 0, 0)]
    [InlineData("255", "255", "255", 255, 255, 255)]
    [InlineData("12", "34", "56", 12, 34, 56)]
    [InlineData(" 12 ", " 34 ", " 56 ", 12, 34, 56)]
    public void TryParseTriplet_ValidInputs_ReturnsColor(string r, string g, string b, byte expectedR, byte expectedG, byte expectedB)
    {
        var parsed = RgbColor.TryParseTriplet(r, g, b, out var color);

        Assert.True(parsed);
        Assert.Equal(new RgbColor(expectedR, expectedG, expectedB), color);
    }

    [Theory]
    [InlineData("256", "0", "0")]
    [InlineData("-1", "0", "0")]
    [InlineData("0", "abc", "0")]
    [InlineData(null, "0", "0")]
    [InlineData("0", "0", "")]
    [InlineData("0", " ", "0")]
    [InlineData("1.5", "0", "0")]
    public void TryParseTriplet_InvalidInputs_ReturnsFalse(string? r, string? g, string? b)
    {
        var parsed = RgbColor.TryParseTriplet(r, g, b, out var color);

        Assert.False(parsed);
        Assert.Equal(default, color);
    }

    [Theory]
    [InlineData("241,214,207", 241, 214, 207)]
    [InlineData("241, 214, 207", 241, 214, 207)]
    [InlineData("  241  ,  214  ,  207  ", 241, 214, 207)]
    [InlineData("241,214,207 // #f1d6cf", 241, 214, 207)]
    [InlineData("241,214,207,additional,parts", 241, 214, 207)]
    public void TryParseCsv_ValidInputs_ReturnsColor(string csv, byte r, byte g, byte b)
    {
        var parsed = RgbColor.TryParseCsv(csv, out var color);

        Assert.True(parsed);
        Assert.Equal(new RgbColor(r, g, b), color);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("241,214")]
    [InlineData("abc,def,ghi")]
    [InlineData("256,0,0")]
    public void TryParseCsv_InvalidInputs_ReturnsFalse(string? csv)
    {
        var parsed = RgbColor.TryParseCsv(csv, out var color);

        Assert.False(parsed);
        Assert.Equal(default, color);
    }

    [Theory]
    [InlineData(0, 0, 0, "#000000")]
    [InlineData(255, 255, 255, "#FFFFFF")]
    [InlineData(0, 255, 16, "#00FF10")]
    [InlineData(241, 214, 207, "#F1D6CF")]
    public void Hex_FormatsAsUppercaseWithHash(byte r, byte g, byte b, string expected)
    {
        var color = new RgbColor(r, g, b);

        Assert.Equal(expected, color.Hex);
    }

    [Fact]
    public void TryParseTriplet_TurkishCulture_ParsesCultureIndependently()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            var parsed = RgbColor.TryParseTriplet("128", "64", "200", out var color);

            Assert.True(parsed);
            Assert.Equal(new RgbColor(128, 64, 200), color);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void TryParseHex_TurkishCulture_ParsesCultureIndependently()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            var parsed = RgbColor.TryParseHex("#ABCDEF", out var color);

            Assert.True(parsed);
            Assert.Equal(new RgbColor(0xAB, 0xCD, 0xEF), color);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void Record_EqualityByValue()
    {
        var a = new RgbColor(10, 20, 30);
        var b = new RgbColor(10, 20, 30);
        var c = new RgbColor(10, 20, 31);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }
}
