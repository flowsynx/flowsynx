using System.Collections.Concurrent;

namespace FlowSynx.PluginCore;

public class PluginContext
{
    private readonly ConcurrentDictionary<string, object> _data = new();

    public Dictionary<string, object> GetAll() => _data.ToDictionary(entry => entry.Key, entry => entry.Value);

    public void SetData(string key, object value)
    {
        _data[key] = value;
    }

    public T GetData<T>(string key)
    {
        if (_data.TryGetValue(key, out var value))
        {
            return (T)value;
        }

        throw new KeyNotFoundException($"No data found for key: {key}");
    }

    public bool TryGetData<T>(string key, out T value)
    {
        if (_data.TryGetValue(key, out var result))
        {
            value = (T)result;
            return true;
        }

        value = default;
        return false;
    }
}
