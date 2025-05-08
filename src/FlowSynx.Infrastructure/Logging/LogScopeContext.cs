namespace FlowSynx.Infrastructure.Logging;

public class LogScopeContext : IReadOnlyCollection<KeyValuePair<string, object>>
{
    private readonly Dictionary<string, object> _values = new();

    public void Add(string key, object value)
    {
        _values[key] = value;
    }

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _values.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _values.Count;
}