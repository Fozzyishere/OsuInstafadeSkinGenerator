namespace OsuInstaFadeSkinGenerator.Services;

public sealed class ValidationState
{
    private readonly Dictionary<string, string> errors = new();

    public bool HasErrors => this.errors.Count > 0;

    public string DisplayText => string.Join(Environment.NewLine, this.errors.Values);

    public bool TrySet(string key, string message)
    {
        if (this.errors.TryGetValue(key, out var existing) && existing == message)
        {
            return false;
        }

        this.errors[key] = message;
        return true;
    }

    public bool Clear(string key)
    {
        return this.errors.Remove(key);
    }
}
