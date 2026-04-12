using OsuInstaFadeSkinGenerator.Services;

namespace OsuInstaFadeSkinGenerator.Tests;

public sealed class SkinIniWriterTests
{
    [Fact]
    public void Update_Template2_RewritesToSingleComboAndPreservesUnrelatedLines()
    {
        using var skinDir = new TestSkinDirectory();
        SkinTestHelper.WriteSkinIni(skinDir.RootPath, Template2SkinIni);

        var writer = new SkinIniWriter();
        var skinIniPath = Path.Combine(skinDir.RootPath, "skin.ini");

        writer.Update(skinIniPath, 7, 8, 9, 64);

        var updated = File.ReadAllText(skinIniPath);

        Assert.Contains("Name: - JesusOmega {NM} 『Planets』 -", updated);
        Assert.Contains("Profile: https://osu.ppy.sh/users/11080396", updated);
        Assert.Contains("    Combo1: 7,8,9", updated);
        Assert.DoesNotContain("Combo2:", updated);
        Assert.DoesNotContain("Combo3:", updated);
        Assert.DoesNotContain("Combo4:", updated);
        Assert.Contains("    MenuGlow: 145,191,255 // #91bfff", updated);
        Assert.Contains("    MenuGlow: 17,157,244 // #119df4", updated);
        Assert.Contains("    SliderBorder: 205,192,236 // #cdc0ec", updated);
        Assert.Contains("    SliderTrackOverride: 10,10,10 // #0a0a0a", updated);
        Assert.Contains("    HitCirclePrefix: default", updated);
        Assert.Contains("    HitCircleOverlap: 64", updated);
        Assert.Contains("    ScorePrefix: score", updated);
        Assert.Contains("    ComboPrefix: score", updated);
    }

    [Fact]
    public void Update_Template3_PreservesMultipleManiaSectionsAndNoisyFormatting()
    {
        using var skinDir = new TestSkinDirectory();
        SkinTestHelper.WriteSkinIni(skinDir.RootPath, Template3SkinIni);

        var writer = new SkinIniWriter();
        var skinIniPath = Path.Combine(skinDir.RootPath, "skin.ini");

        writer.Update(skinIniPath, 4, 5, 6, 77);

        var updated = File.ReadAllText(skinIniPath);

        Assert.Equal(2, updated.Split("[Mania]", StringSplitOptions.None).Length - 1);
        Assert.Contains("    Combo1: 4,5,6", updated);
        Assert.DoesNotContain("Combo2:", updated);
        Assert.DoesNotContain("Combo3:", updated);
        Assert.Contains("    HitCirclePrefix: default", updated);
        Assert.Contains("    HitCircleOverlap: 77", updated);
        Assert.Contains("    ScorePrefix: score", updated);
        Assert.Contains("    ComboPrefix: combo", updated);
        Assert.Contains("    Keys: 1", updated);
        Assert.Contains("    Keys: 2", updated);
        Assert.Contains("    ColumnSpacing: 5", updated);
        Assert.Contains(@"    NoteImage0: Orbs\White", updated);
        Assert.Contains(@"    NoteImage1T: Orbs\Holdcapb", updated);
    }

    [Fact]
    public void Update_TemplateDerivedSparseIni_AppendsMissingSectionsAndUsesDetectedIndent()
    {
        using var skinDir = new TestSkinDirectory();
        SkinTestHelper.WriteSkinIni(
            skinDir.RootPath,
            """
            //Formatted by ck // pepega tools // cyperdark#6890
            [General]
                Name: - Example Skin
                Author: cyperdark
                ||=====
                || Downloaded from https://ck1t.ru/ss
                ||=====
                Version: latest
            """);

        var writer = new SkinIniWriter();
        var skinIniPath = Path.Combine(skinDir.RootPath, "skin.ini");

        writer.Update(skinIniPath, 1, 2, 3, 55);

        var updated = File.ReadAllText(skinIniPath);

        Assert.Contains("[Colours]", updated);
        Assert.Contains("    Combo1: 1,2,3", updated);
        Assert.Contains("[Fonts]", updated);
        Assert.Contains("    HitCircleOverlap: 55", updated);
        Assert.Contains("    Name: - Example Skin", updated);
        Assert.Contains("    Author: cyperdark", updated);
        Assert.Contains("    || Downloaded from https://ck1t.ru/ss", updated);
    }

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
}
