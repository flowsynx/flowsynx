namespace FlowSynx.Application.Configuration.Core.Database;

public abstract class DatabaseConnection
{
    public string Provider { get; set; } = string.Empty;
    public string? ConnectionString { get; protected set; }

    public abstract void BuildConnectionString();
}