namespace FlowSynx.Infrastructure.Configuration.Database;

public class DatabaseConfiguration
{
    public string Default { get; set; } = "SQLite";
    public Dictionary<string, DatabaseConnection> Connections { get; set; } = new();

    public DatabaseConnection GetActiveConnection()
    {
        if (Connections.TryGetValue(Default, out var connection))
        {
            connection.BuildConnectionString();
            return connection;
        }

        var fallback = new SqliteDatabaseConnection { FilePath = "flowsynx.db" };
        fallback.BuildConnectionString();
        return fallback;
    }
}