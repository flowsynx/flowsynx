namespace FlowSynx.Connectors.Database.MySql.Models;

public class DeleteOptions
{
    public string Table { get; set; } = string.Empty;
    public string? Filter { get; set; }
    public bool? Purge { get; set; } = false;
}