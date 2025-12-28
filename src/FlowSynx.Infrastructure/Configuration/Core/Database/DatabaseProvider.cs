namespace FlowSynx.Infrastructure.Configuration.Core.Database;

public class DatabaseProvider(string name) : IDatabaseProvider
{
    public string Name { get; } = name;
}