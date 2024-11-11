namespace FlowSynx.Connectors.Database.MySql.Models;

public class Filter
{
    public LogicOperator? Operator { get; set; } = LogicOperator.And;
    public ComparisonOperator Comparison { get; set; }
    public required string Name { get; set; }
    public string? Value { get; set; }
    public string? ValueMax { get; set; }
    public List<Filter>? Filters { get; set; } = new List<Filter>();
}