namespace OsuInstaFadeSkinGenerator.Tests;

internal sealed class TestSkinDirectory : IDisposable
{
    private const int RetryDelayMilliseconds = 100;

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

        try
        {
            Directory.Delete(this.RootPath, recursive: true);
        }
        catch (IOException)
        {
            Thread.Sleep(RetryDelayMilliseconds);
            try
            {
                Directory.Delete(this.RootPath, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
        catch (UnauthorizedAccessException)
        {
            Thread.Sleep(RetryDelayMilliseconds);
            try
            {
                Directory.Delete(this.RootPath, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
