namespace FlowSynx.Application.AI;

public interface IAgentTool
{
    string Name { get; }

    AgentToolDescriptor GetDescriptor();

    Task<AgentToolResult> ExecuteAsync(
        string operationName, 
        Dictionary<string, object?>? args, 
        CancellationToken cancellationToken);
}