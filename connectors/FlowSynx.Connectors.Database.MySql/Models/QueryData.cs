namespace FlowSynx.Connectors.Database.MySql.Models;

public class QueryData
{
    public QueryData()
    {
        Fields = new List<Field>();
        Joins = new List<string>();
        Filter = new List<Filter>();
        Sort = new List<Sort>();
    }

    public required Table Table { get; set; }
    public List<Field>? Fields { get; set; }
    public List<string>? Joins { get; set; }
    public List<Filter>? Filter { get; set; }
    public string? GroupBy { get; set; }
    public List<Sort>? Sort { get; set; }
    public string? Limit { get; set; }
}