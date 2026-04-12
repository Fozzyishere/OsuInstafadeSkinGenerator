namespace OsuInstaFadeSkinGenerator.Tests;

internal sealed class TestSkinDirectory : IDisposable
{
    public TestSkinDirectory()
    {
        this.RootPath = Path.Combine(
            Path.GetTempPath(),
            "OsuInstaFadeSkinGenerator.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(this.RootPath);
    }

    public string RootPath { get; }

    public void Dispose()
    {
        if (Directory.Exists(this.RootPath))
        {
            Directory.Delete(this.RootPath, recursive: true);
        }
    }
}
