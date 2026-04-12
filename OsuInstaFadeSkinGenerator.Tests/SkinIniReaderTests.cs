using OsuInstaFadeSkinGenerator.Services;

namespace OsuInstaFadeSkinGenerator.Tests;

public sealed class SkinIniReaderTests
{
    [Fact]
    public void Read_Template1_ParsesSupportedFieldsFromCommentHeavySkinIni()
    {
        using var skinDir = new TestSkinDirectory();
        SkinTestHelper.WriteSkinIni(skinDir.RootPath, Template1SkinIni);

        var reader = new SkinIniReader();
        var config = reader.Read(Path.Combine(skinDir.RootPath, "skin.ini"));

        var combo1 = Assert.Single(config.ComboColours, combo => combo.Index == 1);
        var combo2 = Assert.Single(config.ComboColours, combo => combo.Index == 2);

        Assert.Equal("-         《CK》 WhiteCat 2.1 ~ new", config.Name);
        Assert.Equal("cyperdark", config.Author);
        Assert.Equal("2.5", config.Version);
        Assert.True(config.HitCircleOverlayAboveNumber);
        Assert.Equal("default", config.HitCirclePrefix);
        Assert.Equal(15, config.HitCircleOverlap);
        Assert.Equal((byte)206, combo1.R);
        Assert.Equal((byte)188, combo1.G);
        Assert.Equal((byte)178, combo1.B);
        Assert.Equal((byte)237, combo2.R);
        Assert.Equal((byte)221, combo2.G);
        Assert.Equal((byte)213, combo2.B);
        Assert.NotNull(config.SliderBorder);
        Assert.Equal((byte)80, config.SliderBorder.R);
        Assert.Equal((byte)80, config.SliderBorder.G);
        Assert.Equal((byte)80, config.SliderBorder.B);
        Assert.NotNull(config.SliderTrackOverride);
        Assert.Equal((byte)0, config.SliderTrackOverride.R);
        Assert.Equal((byte)0, config.SliderTrackOverride.G);
        Assert.Equal((byte)0, config.SliderTrackOverride.B);
    }

    [Fact]
    public void Read_Template2_ParsesInlineRgbCommentsAndDuplicateNoise()
    {
        using var skinDir = new TestSkinDirectory();
        SkinTestHelper.WriteSkinIni(skinDir.RootPath, Template2SkinIni);

        var reader = new SkinIniReader();
        var config = reader.Read(Path.Combine(skinDir.RootPath, "skin.ini"));

        var combo1 = Assert.Single(config.ComboColours, combo => combo.Index == 1);
        var combo4 = Assert.Single(config.ComboColours, combo => combo.Index == 4);

        Assert.Equal("- JesusOmega {NM} 『Planets』 -", config.Name);
        Assert.Equal("JesusOmega", config.Author);
        Assert.Equal("latest", config.Version);
        Assert.True(config.HitCircleOverlayAboveNumber);
        Assert.Equal("default", config.HitCirclePrefix);
        Assert.Equal(26, config.HitCircleOverlap);
        Assert.Equal((byte)255, combo1.R);
        Assert.Equal((byte)105, combo1.G);
        Assert.Equal((byte)125, combo1.B);
        Assert.Equal((byte)88, combo4.R);
        Assert.Equal((byte)196, combo4.G);
        Assert.Equal((byte)112, combo4.B);
        Assert.NotNull(config.SliderBorder);
        Assert.Equal((byte)205, config.SliderBorder.R);
        Assert.Equal((byte)192, config.SliderBorder.G);
        Assert.Equal((byte)236, config.SliderBorder.B);
        Assert.NotNull(config.SliderTrackOverride);
        Assert.Equal((byte)10, config.SliderTrackOverride.R);
        Assert.Equal((byte)10, config.SliderTrackOverride.G);
        Assert.Equal((byte)10, config.SliderTrackOverride.B);
    }

    [Fact]
    public void Read_Template3_IgnoresRepeatedManiaSectionsAndKeepsSupportedFields()
    {
        using var skinDir = new TestSkinDirectory();
        SkinTestHelper.WriteSkinIni(skinDir.RootPath, Template3SkinIni);

        var reader = new SkinIniReader();
        var config = reader.Read(Path.Combine(skinDir.RootPath, "skin.ini"));

        var combo1 = Assert.Single(config.ComboColours, combo => combo.Index == 1);
        var combo3 = Assert.Single(config.ComboColours, combo => combo.Index == 3);

        Assert.Equal("BubbleSkin-EditCoquis v2", config.Name);
        Assert.Equal("Various", config.Author);
        Assert.Equal("2.0", config.Version);
        Assert.True(config.HitCircleOverlayAboveNumber);
        Assert.Equal("default", config.HitCirclePrefix);
        Assert.Equal(10, config.HitCircleOverlap);
        Assert.Equal((byte)128, combo1.R);
        Assert.Equal((byte)131, combo1.G);
        Assert.Equal((byte)253, combo1.B);
        Assert.Equal((byte)15, combo3.R);
        Assert.Equal((byte)177, combo3.G);
        Assert.Equal((byte)255, combo3.B);
        Assert.NotNull(config.SliderBorder);
        Assert.Equal((byte)70, config.SliderBorder.R);
        Assert.Equal((byte)70, config.SliderBorder.G);
        Assert.Equal((byte)70, config.SliderBorder.B);
        Assert.NotNull(config.SliderTrackOverride);
        Assert.Equal((byte)0, config.SliderTrackOverride.R);
        Assert.Equal((byte)0, config.SliderTrackOverride.G);
        Assert.Equal((byte)20, config.SliderTrackOverride.B);
    }

    [Fact]
    public void Read_Template4_ReadsHitCircleOverlayFlagAndCommentedColours()
    {
        using var skinDir = new TestSkinDirectory();
        SkinTestHelper.WriteSkinIni(skinDir.RootPath, Template4SkinIni);

        var reader = new SkinIniReader();
        var config = reader.Read(Path.Combine(skinDir.RootPath, "skin.ini"));

        var combo1 = Assert.Single(config.ComboColours, combo => combo.Index == 1);

        Assert.Equal("-         《CK》 Bacon boi 1.0", config.Name);
        Assert.Equal("cyperdark", config.Author);
        Assert.Equal("2.5", config.Version);
        Assert.False(config.HitCircleOverlayAboveNumber);
        Assert.Equal("default", config.HitCirclePrefix);
        Assert.Equal(25, config.HitCircleOverlap);
        Assert.Equal((byte)241, combo1.R);
        Assert.Equal((byte)214, combo1.G);
        Assert.Equal((byte)207, combo1.B);
        Assert.NotNull(config.SliderBorder);
        Assert.Equal((byte)113, config.SliderBorder.R);
        Assert.Equal((byte)102, config.SliderBorder.G);
        Assert.Equal((byte)98, config.SliderBorder.B);
        Assert.NotNull(config.SliderTrackOverride);
        Assert.Equal((byte)20, config.SliderTrackOverride.R);
        Assert.Equal((byte)18, config.SliderTrackOverride.G);
        Assert.Equal((byte)17, config.SliderTrackOverride.B);
    }

    [Fact]
    public void Read_TemplateDerivedMixedCasingAndInvalidValues_LeavesDefaults()
    {
        using var skinDir = new TestSkinDirectory();
        var content = Template4SkinIni
            .Replace("[General]", "[gEnErAl]", StringComparison.Ordinal)
            .Replace("[Colours]", "[cOlOuRs]", StringComparison.Ordinal)
            .Replace("[Fonts]", "[fOnTs]", StringComparison.Ordinal)
            .Replace("HitCircleOverlayAboveNumber: 0", "HitCircleOverlayAboveNumer: maybe", StringComparison.Ordinal)
            .Replace("Combo1: 241,214,207 // #f1d6cf", "combo1: nope", StringComparison.Ordinal)
            .Replace("HitCirclePrefix: default", "HitCirclePrefix:", StringComparison.Ordinal)
            .Replace("HitCircleOverlap: 25", "HitCircleOverlap: invalid", StringComparison.Ordinal);
        SkinTestHelper.WriteSkinIni(skinDir.RootPath, content);

        var reader = new SkinIniReader();
        var config = reader.Read(Path.Combine(skinDir.RootPath, "skin.ini"));

        var combo2 = Assert.Single(config.ComboColours, combo => combo.Index == 2);

        Assert.Equal("-         《CK》 Bacon boi 1.0", config.Name);
        Assert.Equal("cyperdark", config.Author);
        Assert.Equal("2.5", config.Version);
        Assert.True(config.HitCircleOverlayAboveNumber);
        Assert.Equal((byte)206, combo2.R);
        Assert.Equal((byte)137, combo2.G);
        Assert.Equal((byte)137, combo2.B);
        Assert.Equal("default", config.HitCirclePrefix);
        Assert.Equal(-2, config.HitCircleOverlap);
    }

    private const string Template1SkinIni =
        """
        //Formatted by ck // pepega tools // cyperdark#6890 // https://github.com/cyperdark/ck-tools
        [General]
            Name: -         《CK》 WhiteCat 2.1 ~ new
            Author: cyperdark
            Profile: https://osu.ppy.sh/users/9893708
            ||=====
            || Downloaded from https://ck1t.ru/ss
            || Skin https://ck1t.ru/s-WhiteCat 2.1 (CK)
            ||=====

            Version: 2.5
            AnimationFramerate: 60

        // -=- Slider 
            AllowSliderBallTint: 1

        // -=- Combo bursts 
            ComboBurstRandom: 0
            SliderBallFlip: 1

        // -=- Cursor 
            CursorExpand: 0
            CursorCentre: 1
            CursorRotate: 0
            CursorTrailRotate: 0

        [Colours]
        // -=- Combo Colors 
            Combo1: 206, 188, 178 // #CEBCB2
            Combo2: 237, 221, 213 // #EDDDD5

        // -=- Misc Colors 
            InputOverlayText: 254, 237, 227 // #FEEDE3

        // -=- Song Select 
            SongSelectActiveText: 255, 255, 255 // #FFFFFF
            SongSelectInactiveText: 200, 200, 200 // #C8C8C8
            MenuGlow: 205, 186, 177 // #CDBAB1

        // -=- Colors 
            SpinnerBackground: 255, 255, 255 // #FFFFFF

        // -=- Slider Colors 
            SliderBorder: 80, 80, 80 // #505050
            SliderTrackOverride: 0, 0, 0 // #000000

        [Fonts]
        // -=- HitCircle 
            HitCircleOverlap: 15

        // -=- Score 
            ScorePrefix: numbers
            ScoreOverlap: 0
            ComboPrefix: numbers
            ComboOverlap: 0
        """;

    private const string Template2SkinIni =
        """
        //Formatted by ck // pepega tools // cyperdark#6890
        [General]
            Name: - JesusOmega {NM} 『Planets』 -
            Author: JesusOmega
            Profile: https://osu.ppy.sh/users/11080396
            ╔=====================================╗
            ║ Downloaded from https://ck1t.ru/ss  ╚================╗
            ║ Skin https://ck1t.ru/s-- JesusOmega {NM} 『Planets』 - ║
            ╚======================================================╝
            Version: latest
            AnimationFramerate: 46

        // ╔════ Cursor ════╗ \\
            CursorCentre: 1
            CursorExpand: 1
            CursorRotate: 0

        // ╔════ Combo bursts ════╗ \\
            ComboBurstRandom: 0

        // ╔════ Slider ════╗ \\
            AllowSliderBallTint: 1
            SliderBallFlip: 0
            SliderBallFrames: 1
            SliderStyle: 2

        // ╔════ Spinner ════╗ \\
            SpinnerNoBlink: 1

        [Colours]
        // ╔════ Combo Colors ════╗ \\
            Combo1: 255, 105, 125 // #ff697d
            Combo2: 224, 177, 252 // #e0b1fc
            Combo3: 131, 180, 252 // #83b4fc
            Combo4: 88, 196, 112 // #58c470
            MenuGlow: 145,191,255 // #91bfff
            MenuGlow: 17,157,244 // #119df4
            InputOverlayText: 255,255,255 // #ffffff
            SliderBorder: 205,192,236 // #cdc0ec
            SliderTrackOverride: 10,10,10 // #0a0a0a

        // ╔════ Song Select ════╗ \\
            SongSelectActiveText: 130,203,255 // #82cbff
            SongSelectInactiveText: 120,120,120 // #787878

        [Fonts]
        // ╔════ HitCircle ════╗ \\
            HitCirclePrefix: default
            HitCircleOverlap: 26

        // ╔════ Score ════╗ \\
            ScorePrefix: score
            ScoreOverlap: 8

        // ╔════ Combo ════╗ \\
            ComboPrefix: score
        """;

    private const string Template3SkinIni =
        """
        // Ini parser v0.2.9 // cyperdark#6890 // https://github.com/cyperdark/ck-tools


        [General]
            Name: BubbleSkin-EditCoquis v2
            Author: Various
            Version: 2.0

        // -=- Cursor
            CursorCentre: 1
            CursorExpand: 0
            CursorRotate: 0
            CursorTrailRotate: 0

        // -=- Slider
            AllowSliderBallTint: 1
            SliderBallFlip: 1

        // -=- Spinner
            SpinnerFadePlayfield: 1


        [Colours]
        // -=- Combo Colors
            Combo1: 128,131,253
            Combo2: 130,253,207
            Combo3: 15,177,255

        // -=- Misc Colors
            InputOverlayText: 255,255,255

        // -=- Slider Colors
            SliderBorder: 70,70,70
            SliderTrackOverride: 0,0,20

        // -=- Song Select
            SongSelectActiveText: 250,250,250
            SongSelectInactiveText: 230,230,230


        [Fonts]
        // -=- Circle Font
            HitCirclePrefix: default
            HitCircleOverlap: 10

        // -=- Score Font
            ScorePrefix: score
            ScoreOverlap: 7

        // -=- Combo Font
            ComboPrefix: combo
            ComboOverlap: 3


        [Mania]
            Keys: 1
            WarningArrow: arrow-scroll
            BarlineHeight: 0
            LightFramePerSecond: 60

        // -=- Toggles
            KeysUnderNotes: 1
            JudgementLine: 0
            UpsideDown: 0
            SpecialStyle: 0

        // -=- Position
            HitPosition: 458
            ScorePosition: 280
            ComboPosition: 160

        // -=- Column
            ColumnStart: 393
            ColumnLineWidth: 0,0
            ColumnWidth: 68

        // -=- Lighting
            LightingNWidth: 62
            LightingLWidth: 62
            LightingN: lightingA
            LightingL: lightingA

        // -=- Stage
            StageHint: blank-hint

        // -=- Note Style
            NoteBodyStyle: 0

        // -=- Colors
            Colour1: 0,0,0,0
            ColourBarline: 250,175,255,150
            ColourColumnLine: 30,30,30,30
            ColourHold: 255,255,255,255

        // -=- Key Image
            KeyImage0: Orbs\k_down
            KeyImage0D: Orbs\k_down

        // -=- Note Image
            NoteImage0: Orbs\White
            NoteImage0H: Orbs\White
            NoteImage0L: Orbs\Holdbodyw
            NoteImage0T: Orbs\Holdcapw


        [Mania]
            Keys: 2
            WarningArrow: arrow-scroll
            BarlineHeight: 0
            LightFramePerSecond: 60

        // -=- Toggles
            KeysUnderNotes: 1
            JudgementLine: 0
            UpsideDown: 0
            SpecialStyle: 0

        // -=- Position
            HitPosition: 458
            ScorePosition: 280
            ComboPosition: 160

        // -=- Column
            ColumnStart: 356
            ColumnLineWidth: 0,0,0
            ColumnWidth: 68,68
            ColumnSpacing: 5

        // -=- Lighting
            LightingNWidth: 62,62
            LightingLWidth: 62,62
            LightingN: lightingA
            LightingL: lightingA

        // -=- Stage
            StageHint: blank-hint

        // -=- Note Style
            NoteBodyStyle: 0

        // -=- Colors
            Colour1: 0,0,0,0
            Colour2: 0,0,0,0
            ColourBarline: 250,175,255,150
            ColourColumnLine: 30,30,30,30
            ColourHold: 255,255,255,255

        // -=- Key Image
            KeyImage0: Orbs\k_down
            KeyImage0D: Orbs\k_down
            KeyImage1: Orbs\k_down
            KeyImage1D: Orbs\k_down

        // -=- Note Image
            NoteImage0: Orbs\White
            NoteImage0H: Orbs\White
            NoteImage0L: Orbs\Holdbodyw
            NoteImage1: Orbs\Blue
            NoteImage1H: Orbs\Blue
            NoteImage1L: Orbs\Holdbodyb
            NoteImage0T: Orbs\Holdcapw
            NoteImage1T: Orbs\Holdcapb
        """;

    private const string Template4SkinIni =
        """
        //Formatted by ck // pepega tools // cyperdark#6890
        [General]
            Name: -         《CK》 Bacon boi 1.0
            Author: cyperdark
            Profile: https://osu.ppy.sh/users/9893708
            ╔=====================================╗
            ║ Downloaded from https://ck1t.ru/ss  ╚===============╗
            ║ Skin https://ck1t.ru/s--         《CK》 Bacon boi 1.0 ║
            ╚=====================================================╝
            Version: 2.5
            AnimationFramerate: 60

        // ╔════ Cursor ════╗ \\
            CursorCentre: 1
            CursorExpand: 0
            CursorRotate: 0
            CursorTrailRotate: 0

        // ╔════ Combo bursts ════╗ \\
            ComboBurstRandom: 0
            HitCircleOverlayAboveNumber: 0

        // ╔════ Slider ════╗ \\
            AllowSliderBallTint: 1
            SliderBallFlip: 1
            SliderStyle: 2

        [Colours]
        // ╔════ Combo Colors ════╗ \\
            Combo1: 241,214,207 // #f1d6cf
            Combo2: 206,137,137 // #ce8989
            MenuGlow: 82, 74, 71 // #524a47
            InputOverlayText: 255, 255, 255 // #ffffff
            SliderBorder: 113, 102, 98 // #716662
            SliderTrackOverride: 20, 18, 17 // #141211

        // ╔════ Song Select ════╗ \\
            SongSelectActiveText: 255, 255, 255 // #ffffff
            SongSelectInactiveText: 255, 255, 255 // #ffffff
            SpinnerBackground: 255, 255, 255 // #ffffff

        [Fonts]
        // ╔════ HitCircle ════╗ \\
            HitCirclePrefix: default
            HitCircleOverlap: 25

        // ╔════ Score ════╗ \\
            ScorePrefix: numbers
            ScoreOverlap: 10

        // ╔════ Combo ════╗ \\
            ComboPrefix: numbers
            ComboOverlap: 10
        """;
}
