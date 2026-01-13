using FlowSynx.Infrastructure.Persistence.Abstractions;

namespace FlowSynx.Configuration.Database;

public class DatabaseProvider(string name) : IDatabaseProvider
{
    public string Name { get; } = name;
}