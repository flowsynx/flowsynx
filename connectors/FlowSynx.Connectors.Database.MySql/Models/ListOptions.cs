namespace FlowSynx.Connectors.Database.MySql.Models;

public class ListOptions
{
    public string Table { get; set; } = string.Empty;
    public string? Distinct { get; set; }
    public string? Fields { get; set; }
    public string? Joins { get; set; }
    public string? Filter { get; set; }
    public string? GroupBy { get; set; }
    public string? Sort { get; set; }
    public string? Limit { get; set; }
}