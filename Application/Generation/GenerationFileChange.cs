namespace OsuInstaFadeSkinGenerator.Application.Generation;

internal sealed class GenerationFileChange
{
    public GenerationFileChange(string targetPath, string stagedPath)
    {
        this.TargetPath = targetPath;
        this.StagedPath = stagedPath;
    }

    public string TargetPath { get; }

    public string StagedPath { get; }

    public bool OriginalExists { get; set; }

    public string? SnapshotPath { get; set; }
}
