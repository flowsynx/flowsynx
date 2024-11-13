namespace FlowSynx.Connectors.Database.MySql.Models;

public class SelectQuery
{
    public SelectQuery()
    {
        Fields = new FieldsList();
        Joins = new JoinsList();
        Filters = new FiltersList();
        GroupBy = new GroupByList();
        Sort = new SortsList();
    }

    public required Table Table { get; set; }
    public FieldsList Fields { get; set; }
    public JoinsList? Joins { get; set; }
    public FiltersList? Filters { get; set; }
    public GroupByList? GroupBy { get; set; }
    public SortsList? Sort { get; set; }
    public string? Limit { get; set; }

    public string GetSql(ISqlFormat format)
    {
        var result = TemplateLibrary.Select;
        result.Append(SnippetLibrary.Table(format, Table.Name, Table.Alias));
        result.Append(SnippetLibrary.Fields(Fields.GetSql(format, Table.Alias)));

        if (Joins is { Count: > 0 })
        {
            var joinTable = string.IsNullOrEmpty(Table.Alias) ? Table.Name : Table.Alias;
            result.Append(SnippetLibrary.Join(Joins.GetSql(format, joinTable)));
        }

        if (Filters is { Count: > 0 })
            result.Append(SnippetLibrary.Filters(Filters.GetSql(format, Table.Alias)));

        if (GroupBy is { Count: > 0 })
            result.Append(SnippetLibrary.GroupBy(GroupBy.GetSql(format, Table.Alias)));

        if (Sort is { Count: > 0 })
            result.Append(SnippetLibrary.Sort(this.Sort.GetSql(format, Table.Alias)));

        return result.GetSql(format);
    }
}