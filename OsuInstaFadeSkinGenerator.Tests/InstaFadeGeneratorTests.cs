using OsuInstaFadeSkinGenerator.Models;
using OsuInstaFadeSkinGenerator.Services;
using SixLabors.ImageSharp.PixelFormats;

namespace OsuInstaFadeSkinGenerator.Tests;

public sealed class InstaFadeGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Template2_PerformsInstaFadeWorkflowAndUpdatesSkinIni()
    {
        using var skinDir = new TestSkinDirectory();
        const string prefix = "default";
        SkinTestHelper.WriteSkinIni(skinDir.RootPath, Template2SkinIni);
        CreateBaseAssets(skinDir.RootPath);
        SkinTestHelper.CreateNumberAssets(skinDir.RootPath, prefix);

        var generator = CreateGenerator();
        var result = await generator.GenerateAsync(
            CreateRequest(skinDir.RootPath, processHd: false, backupFiles: false, enableTripleStacking: false));

        Assert.True(result.Success, result.Message);

        var blankPath = SkinTestHelper.ResolvePrefixPath(skinDir.RootPath, prefix, "0");
        var numberPath = SkinTestHelper.ResolvePrefixPath(skinDir.RootPath, prefix, "1");
        Assert.True(File.Exists(blankPath));
        Assert.True(File.Exists(numberPath));

        using (var blank = SkinTestHelper.LoadPng(blankPath))
        {
            Assert.Equal(5, blank.Width);
            Assert.Equal(5, blank.Height);
            for (int y = 0; y < blank.Height; y++)
            {
                for (int x = 0; x < blank.Width; x++)
                {
                    Assert.Equal((byte)0, blank[x, y].A);
                }
            }
        }

        using (var number = SkinTestHelper.LoadPng(numberPath))
        {
            Assert.Equal(5, number.Width);
            Assert.Equal(5, number.Height);
        }

        SkinTestHelper.AssertFullyTransparent(Path.Combine(skinDir.RootPath, "hitcircle.png"));
        SkinTestHelper.AssertFullyTransparent(Path.Combine(skinDir.RootPath, "hitcircleoverlay.png"));

        var updatedSkinIni = File.ReadAllText(Path.Combine(skinDir.RootPath, "skin.ini"));
        Assert.Contains("Combo1: 10,20,30", updatedSkinIni);
        Assert.DoesNotContain("Combo2:", updatedSkinIni);
        Assert.DoesNotContain("Combo3:", updatedSkinIni);
        Assert.DoesNotContain("Combo4:", updatedSkinIni);
        Assert.Contains("HitCircleOverlap: 5", updatedSkinIni);
        Assert.Contains("HitCirclePrefix: default", updatedSkinIni);
        Assert.Contains("ScorePrefix: score", updatedSkinIni);
        Assert.Contains("MenuGlow: 145,191,255 // #91bfff", updatedSkinIni);
        Assert.Contains("MenuGlow: 17,157,244 // #119df4", updatedSkinIni);
    }

    [Fact]
    public async Task GenerateAsync_Template4_BacksUpOriginalAssetsGeneratedNumbersAndSkinIni()
    {
        using var skinDir = new TestSkinDirectory();
        const string prefix = "default";
        SkinTestHelper.WriteSkinIni(skinDir.RootPath, Template4SkinIni);
        CreateBaseAssets(skinDir.RootPath);
        SkinTestHelper.WriteFilledPng(Path.Combine(skinDir.RootPath, "hitcircle@2x.png"), 8, 8, new Rgba32(200, 0, 0, 255));
        SkinTestHelper.WriteFilledPng(Path.Combine(skinDir.RootPath, "hitcircleoverlay@2x.png"), 8, 8, new Rgba32(0, 200, 0, 255));
        SkinTestHelper.WriteFilledPng(SkinTestHelper.ResolvePrefixPath(skinDir.RootPath, prefix, "1"), 2, 2, new Rgba32(255, 255, 255, 255));
        SkinTestHelper.WriteFilledPng(SkinTestHelper.ResolvePrefixPath(skinDir.RootPath, prefix, "5", "@2x"), 4, 4, new Rgba32(255, 255, 255, 255));

        var generator = CreateGenerator();

        var result = await generator.GenerateAsync(
            CreateRequest(skinDir.RootPath, processHd: false, backupFiles: true, enableTripleStacking: false));

        Assert.True(result.Success, result.Message);

        var backupDir = Path.Combine(skinDir.RootPath, "_insta-fade-backup");
        Assert.True(File.Exists(Path.Combine(backupDir, "hitcircle.png")));
        Assert.True(File.Exists(Path.Combine(backupDir, "hitcircleoverlay.png")));
        Assert.True(File.Exists(Path.Combine(backupDir, "hitcircle@2x.png")));
        Assert.True(File.Exists(Path.Combine(backupDir, "hitcircleoverlay@2x.png")));
        Assert.True(File.Exists(Path.Combine(backupDir, "default-1.png")));
        Assert.True(File.Exists(Path.Combine(backupDir, "default-5@2x.png")));
        Assert.True(File.Exists(Path.Combine(backupDir, "skin.ini")));

        var backedUpSkinIni = File.ReadAllText(Path.Combine(backupDir, "skin.ini"));
        Assert.Contains("Combo2: 206,137,137 // #ce8989", backedUpSkinIni);
        Assert.Contains("HitCircleOverlayAboveNumber: 0", backedUpSkinIni);
    }

    [Fact]
    public async Task GenerateAsync_Template1_SkipsHdWhenRequestedWithoutRequiredHdAssets()
    {
        using var skinDir = new TestSkinDirectory();
        const string prefix = "default";
        SkinTestHelper.WriteSkinIni(skinDir.RootPath, Template1SkinIni);
        CreateBaseAssets(skinDir.RootPath);
        SkinTestHelper.CreateNumberAssets(skinDir.RootPath, prefix);
        var progress = new RecordingProgress();
        var generator = CreateGenerator();

        var result = await generator.GenerateAsync(
            CreateRequest(skinDir.RootPath, processHd: true, backupFiles: false, enableTripleStacking: false),
            progress);

        Assert.True(result.Success, result.Message);
        Assert.Contains(progress.Entries, entry => entry.Message.Contains("Missing required HD asset", StringComparison.Ordinal));
        Assert.Contains(progress.Entries, entry => entry.Message.Contains("Skipping HD generation", StringComparison.Ordinal));
        Assert.False(File.Exists(SkinTestHelper.ResolvePrefixPath(skinDir.RootPath, prefix, "1", "@2x")));

        var updatedSkinIni = File.ReadAllText(Path.Combine(skinDir.RootPath, "skin.ini"));
        Assert.Contains("Combo1: 10,20,30", updatedSkinIni);
        Assert.DoesNotContain("Combo2:", updatedSkinIni);
        Assert.Contains("HitCircleOverlap: 5", updatedSkinIni);
    }

    [Fact]
    public async Task GenerateAsync_Template3_TripleStackingRestoresMergedBaseAssetsAndKeepsMania()
    {
        using var skinDir = new TestSkinDirectory();
        const string prefix = "default";
        SkinTestHelper.WriteSkinIni(skinDir.RootPath, Template3SkinIni);
        CreateBaseAssets(skinDir.RootPath);
        SkinTestHelper.CreateNumberAssets(skinDir.RootPath, prefix);
        var generator = CreateGenerator();

        var result = await generator.GenerateAsync(
            CreateRequest(skinDir.RootPath, processHd: false, backupFiles: false, enableTripleStacking: true));

        Assert.True(result.Success, result.Message);

        using var hitcircle = SkinTestHelper.LoadPng(Path.Combine(skinDir.RootPath, "hitcircle.png"));
        using var overlay = SkinTestHelper.LoadPng(Path.Combine(skinDir.RootPath, "hitcircleoverlay.png"));

        Assert.Equal(4, hitcircle.Width);
        Assert.Equal(4, hitcircle.Height);
        Assert.Equal(4, overlay.Width);
        Assert.Equal(4, overlay.Height);
        Assert.Equal(hitcircle[0, 0], overlay[0, 0]);
        Assert.Equal(hitcircle[2, 2], overlay[2, 2]);
        Assert.Equal(new Rgba32(255, 0, 0, 255), hitcircle[0, 0]);
        Assert.Equal(new Rgba32(0, 255, 0, 255), hitcircle[2, 2]);

        var updatedSkinIni = File.ReadAllText(Path.Combine(skinDir.RootPath, "skin.ini"));
        Assert.Equal(2, updatedSkinIni.Split("[Mania]", StringSplitOptions.None).Length - 1);
        Assert.Contains("Keys: 1", updatedSkinIni);
        Assert.Contains("Keys: 2", updatedSkinIni);
        Assert.Contains("Combo1: 10,20,30", updatedSkinIni);
        Assert.DoesNotContain("Combo2:", updatedSkinIni);
        Assert.DoesNotContain("Combo3:", updatedSkinIni);
    }

    private static InstaFadeGenerator CreateGenerator()
    {
        return new InstaFadeGenerator(new SkinIniReader(), new SkinIniWriter());
    }

    private static GenerationRequest CreateRequest(
        string skinFolder,
        bool processHd,
        bool backupFiles,
        bool enableTripleStacking)
    {
        return new GenerationRequest(
            skinFolder,
            10,
            20,
            30,
            processHd,
            backupFiles,
            enableTripleStacking);
    }

    private static void CreateBaseAssets(string skinFolder)
    {
        SkinTestHelper.WriteFilledPng(Path.Combine(skinFolder, "hitcircle.png"), 4, 4, new Rgba32(255, 0, 0, 255));
        SkinTestHelper.WritePng(
            Path.Combine(skinFolder, "hitcircleoverlay.png"),
            4,
            4,
            image =>
            {
                image[1, 1] = new Rgba32(0, 255, 0, 255);
                image[1, 2] = new Rgba32(0, 255, 0, 255);
                image[2, 1] = new Rgba32(0, 255, 0, 255);
                image[2, 2] = new Rgba32(0, 255, 0, 255);
            });
    }

    private sealed class RecordingProgress : IProgress<GenerationProgress>
    {
        public List<GenerationProgress> Entries { get; } = [];

        public void Report(GenerationProgress value)
        {
            this.Entries.Add(value);
        }
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
