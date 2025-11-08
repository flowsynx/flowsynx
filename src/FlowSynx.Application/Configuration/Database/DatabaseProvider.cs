namespace FlowSynx.Application.Configuration.Database;

public class DatabaseProvider(string name) : IDatabaseProvider
{
    public string Name { get; } = name;
}