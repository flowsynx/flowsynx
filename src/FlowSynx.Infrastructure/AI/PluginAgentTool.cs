using FlowSynx.Application.AI;
using FlowSynx.Infrastructure.Extensions;
using FlowSynx.Infrastructure.PluginHost;

namespace FlowSynx.Infrastructure.AI;

public class PluginAgentTool : IAgentTool
{
    private readonly string _toolName;
    private readonly string _pluginType;
    private readonly string _description;
    private readonly IPluginTypeService _pluginTypeService;
    private readonly string _userId;

    public PluginAgentTool(string toolName, string pluginType, string description, IPluginTypeService pluginTypeService, string userId)
    {
        _toolName = toolName;
        _pluginType = pluginType;
        _description = description;
        _pluginTypeService = pluginTypeService;
        _userId = userId;
    }

    public string Name => _toolName;

    public AgentToolDescriptor GetDescriptor()
    {
        return new AgentToolDescriptor
        {
            Name = _toolName,
            Description = _description,
            Deterministic = false,
            SideEffecting = true,
            Parameters = new Dictionary<string, AgentToolParameter>(StringComparer.OrdinalIgnoreCase)
            {
                ["input"] = new AgentToolParameter
                {
                    Type = "object",
                    Required = false,
                    Description = "Arbitrary plugin input map."
                },
                ["dryRun"] = new AgentToolParameter
                {
                    Type = "boolean",
                    Required = false,
                    Description = "If true, simulate tool call."
                }
            }
        };
    }

    public async Task<AgentToolResult> ExecuteAsync(
        string operationName, 
        Dictionary<string, object?>? args, 
        CancellationToken cancellationToken)
    {
        var dryRun = args.TryGetValue("dryRun", out var drVal) && drVal is bool b && b;
        if (dryRun)
        {
            var plan = new
            {
                tool = _toolName,
                pluginType = _pluginType,
                simulated = true,
                parameters = args
            };
            return AgentToolResult.Ok(plan);
        }

        var plugin = await _pluginTypeService.Get(_userId, _pluginType, null, cancellationToken);
        var pluginParams = args.ToPluginParameters();
        var result = await plugin.ExecuteAsync(operationName, pluginParams, cancellationToken);
        return AgentToolResult.Ok(result);
    }
}
