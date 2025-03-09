namespace FlowSynx.Connectors.Database.MySql.Models;

public class ExistOptions
{
    public string Table { get; set; } = string.Empty;
    public string? Filter { get; set; }
}