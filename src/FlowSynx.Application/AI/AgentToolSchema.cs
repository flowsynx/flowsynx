namespace FlowSynx.Application.AI;

public static class AgentToolSchema
{
    public static object ToOpenAiToolSchema(AgentToolDescriptor descriptor)
    {
        var properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        var required = new List<string>();

        foreach (var kv in descriptor.Parameters)
        {
            var p = kv.Value;
            var prop = new Dictionary<string, object?>
            {
                ["type"] = p.Type,
                ["description"] = p.Description
            };
            if (p.Default is not null) prop["default"] = p.Default;
            properties[kv.Key] = prop;

            if (p.Required) required.Add(kv.Key);
        }

        return new
        {
            type = "function",
            function = new
            {
                name = descriptor.Name,
                description = descriptor.Description,
                parameters = new
                {
                    type = "object",
                    properties,
                    required
                }
            }
        };
    }
}
