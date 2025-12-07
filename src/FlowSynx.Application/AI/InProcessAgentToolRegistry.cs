namespace FlowSynx.Application.AI;

public class InProcessAgentToolRegistry : IAgentToolRegistry
{
    private readonly Dictionary<string, IAgentTool> _tools = new(StringComparer.OrdinalIgnoreCase);

    public InProcessAgentToolRegistry(IEnumerable<IAgentTool> tools)
    {
        foreach (var t in tools)
        {
            _tools[t.Name] = t;
        }
    }

    public IEnumerable<IAgentTool> GetAllTools() => _tools.Values;

    public IAgentTool? GetTool(string name)
        => name is null ? null : (_tools.TryGetValue(name, out var t) ? t : null);

    public IEnumerable<IAgentTool> GetAllowedTools(IEnumerable<string>? allowList, IEnumerable<string>? denyList)
    {
        var allow = allowList?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var deny = denyList?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        IEnumerable<IAgentTool> source = _tools.Values;

        if (allow.Count > 0)
            source = source.Where(t => allow.Contains(t.Name));

        if (deny.Count > 0)
            source = source.Where(t => !deny.Contains(t.Name));

        return source;
    }
}
