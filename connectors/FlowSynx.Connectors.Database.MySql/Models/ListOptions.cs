namespace FlowSynx.Connectors.Database.MySql.Models;

public class ListOptions
{
    public string Table { get; set; } = string.Empty;
    public string? Distinct { get; set; }
    public string? Fields { get; set; }
    public string? Joins { get; set; }
    public string? Filters { get; set; }
    public string? GroupBy { get; set; }
    public string? Sorts { get; set; }
    public string? Limit { get; set; }
}