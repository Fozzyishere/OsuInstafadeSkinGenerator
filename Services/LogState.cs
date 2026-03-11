namespace OsuInstaFadeSkinGenerator.Services;

public sealed class LogState
{
    private const string DefaultMessage = "Ready. Select a skin folder to get started.";
    private readonly List<string> entries = [];

    public bool HasEntries => this.entries.Count > 0;

    public string DisplayText => this.HasEntries
        ? string.Join(Environment.NewLine, this.entries)
        : DefaultMessage;

    public void Append(string message)
    {
        this.entries.Add(message);
    }
}
