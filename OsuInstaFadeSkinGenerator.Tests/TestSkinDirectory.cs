namespace OsuInstaFadeSkinGenerator.Tests;

internal sealed class TestSkinDirectory : IDisposable
{
    private const int RetryDelayMilliseconds = 100;
    private const int MaxDeleteAttempts = 2;

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
        if (!Directory.Exists(this.RootPath))
        {
            return;
        }

        for (var attempt = 1; attempt <= MaxDeleteAttempts; attempt++)
        {
            try
            {
                Directory.Delete(this.RootPath, recursive: true);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                if (attempt == MaxDeleteAttempts)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Test cleanup warning: failed to delete temporary folder '{this.RootPath}' after {MaxDeleteAttempts} attempts. {ex.GetType().Name}: {ex.Message}");
                    return;
                }

                Thread.Sleep(RetryDelayMilliseconds);
            }
        }
    }
}
