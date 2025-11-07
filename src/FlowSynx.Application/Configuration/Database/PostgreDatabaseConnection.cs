namespace FlowSynx.Application.Configuration.Database;

public class PostgreDatabaseConnection : DatabaseConnection
{
    public string? Host { get; set; }
    public int Port { get; set; } = 5432;
    public string? Database { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public Dictionary<string, string>? AdditionalOptions { get; set; }

    public override void BuildConnectionString()
    {
        var options = AdditionalOptions != null
            ? string.Join(";", AdditionalOptions.Select(kv => $"{kv.Key}={kv.Value}"))
            : "";

        ConnectionString = $"Host={Host};Port={Port};Database={Database};Username={UserName};Password={Password};{options}";
    }
}