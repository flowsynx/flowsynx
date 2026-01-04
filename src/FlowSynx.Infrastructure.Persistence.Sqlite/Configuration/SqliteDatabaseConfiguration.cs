using FlowSynx.Infrastructure.Persistence.Abstractions;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Configuration;

public class SqliteDatabaseConnection : DatabaseConnection
{
    public string? FilePath { get; set; } = "flowsynx.db";
    public Dictionary<string, string>? AdditionalOptions { get; set; }

    public override void BuildConnectionString()
    {
        var options = AdditionalOptions != null
            ? string.Join(";", AdditionalOptions.Select(kv => $"{kv.Key}={kv.Value}"))
            : "";

        ConnectionString = $"Data Source={FilePath};{options}";
    }
}