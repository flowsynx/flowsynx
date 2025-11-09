namespace FlowSynx.Infrastructure.AI;

public interface IConfigurableAi
{
    void Configure(Dictionary<string, string> configuration);
}