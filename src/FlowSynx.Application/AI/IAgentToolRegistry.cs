namespace FlowSynx.Application.AI;

public interface IAgentToolRegistry
{
    IEnumerable<IAgentTool> GetAllTools();
    IAgentTool? GetTool(string name);
    IEnumerable<IAgentTool> GetAllowedTools(IEnumerable<string>? allowList, IEnumerable<string>? denyList);
}