namespace FlowSynx.Connectors.Database.MySql.Models;

public class Filter
{
    public FilterOperator? Operator { get; set; } = FilterOperator.And;
    public FilterType Type { get; set; }
    public required string Name { get; set; }
    public string? Value { get; set; }
    public string? ValueMax { get; set; }
    public List<Filter>? Filters { get; set; } = new List<Filter>();
}