using System.Collections.Concurrent;

namespace LinkShorter.Services;

public class ClickBuffer
{
    private readonly ConcurrentDictionary<string, int> _clicks = new();

    public void AddClick(string code)
    {
        _clicks.AddOrUpdate(code, 1, (_, count) => count + 1);
    }

    public void Remove(string code)
    {
        _clicks.TryRemove(code, out _);
    }

    public IDictionary<string, int> Flush()
    {
        var currentClicks = _clicks.ToArray();
        _clicks.Clear();
        return currentClicks.ToDictionary(k => k.Key, v => v.Value);
    }
}