namespace FlowSynx.Connectors.Database.MySql.Models;

public class WriteOptions
{
    public string Table { get; set; } = string.Empty;
    public string? Fields { get; set; }
    public string? Values { get; set; }
}