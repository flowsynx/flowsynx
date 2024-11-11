namespace FlowSynx.Connectors.Database.MySql.Models;

public class SelectQuery
{
    public SelectQuery()
    {
        Fields = new FieldsList();
        Joins = new List<string>();
        Filter = new List<Filter>();
        Sort = new List<Sort>();
    }

    public required Table Table { get; set; }
    public FieldsList Fields { get; set; }
    public List<string>? Joins { get; set; }
    public List<Filter>? Filter { get; set; }
    public string? GroupBy { get; set; }
    public List<Sort>? Sort { get; set; }
    public string? Limit { get; set; }

    public string GetSql()
    {
        var table = Table.GetSql();
        var columns = Fields.GetSql(Table.Alias);
        var query = $"SELECT {columns} FROM {table};";
        return query;
    }

    public override string ToString()
    {
        return GetSql();
    }
}