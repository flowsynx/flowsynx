namespace FlowSynx.Infrastructure.Secrets;

public interface IConfigurableSecret
{
    void Configure(Dictionary<string, string> configuration);
}