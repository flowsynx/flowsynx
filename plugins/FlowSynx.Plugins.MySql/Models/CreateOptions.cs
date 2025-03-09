namespace FlowSynx.Connectors.Database.MySql.Models;

public class CreateOptions
{
    public string Table { get; set; } = string.Empty;
    public string? Fields { get; set; }
}