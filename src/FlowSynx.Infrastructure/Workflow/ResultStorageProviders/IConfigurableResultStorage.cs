namespace FlowSynx.Infrastructure.Workflow.ResultStorageProviders;

public interface IConfigurableResultStorage
{
    void Configure(Dictionary<string, string> configuration, long maxLimitSize);
}