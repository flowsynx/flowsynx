namespace FlowSynx.ApplicationBuilders;

public interface ICliApplicationBuilder
{
    Task<int> RunAsync(string[] args);
}