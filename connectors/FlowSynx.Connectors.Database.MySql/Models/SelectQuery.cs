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

    public string GetSql()
    {
        var result = TemplateLibrary.Select;
        result.Append(SnippetLibrary.Table(Table.Name, Table.Alias));
        result.Append(SnippetLibrary.Fields(Fields.GetSql(Table.Alias)));

        if (Joins is { Count: > 0 })
        {
            var joinTable = string.IsNullOrEmpty(Table.Alias) ? Table.Name : Table.Alias;
            result.Append(SnippetLibrary.Join(Joins.GetSql(joinTable)));
        }

        if (Filters is { Count: > 0 })
            result.Append(SnippetLibrary.Filters(Filters.GetSql(Table.Alias)));

        if (GroupBy is { Count: > 0 })
            result.Append(SnippetLibrary.GroupBy(GroupBy.GetSql(Table.Alias)));

        if (Sort is { Count: > 0 })
            result.Append(SnippetLibrary.Sort(this.Sort.GetSql(Table.Alias)));

        return result.GetSql();



        //var table = Table.GetSql();
        //var columns = Fields.GetSql(Table.Alias);
        //var filters = string.Empty;
        //if (Filters != null)
        //{
        //    filters = Filters.GetSql(Table.Alias);
        //    if (!string.IsNullOrEmpty(filters))
        //        filters = $" WHERE {filters}";
        //}

        //var joins = string.Empty;
        //if (Joins != null)
        //{
        //    var joinTable = string.IsNullOrEmpty(Table.Alias) ? Table.Name : Table.Alias;
        //    joins = Joins.GetSql(joinTable);
        //    if (!string.IsNullOrEmpty(joins))
        //        joins = $" {joins}";
        //}

        //var groupBy = string.Empty;
        //if (GroupBy != null)
        //{
        //    groupBy = GroupBy.GetSql(Table.Alias);
        //    if (!string.IsNullOrEmpty(groupBy))
        //        groupBy = $" GROUP BY {groupBy}";
        //}

        //var sorts = string.Empty;
        //if (Sort != null)
        //{
        //    sorts = Sort.GetSql(Table.Alias);
        //    if (!string.IsNullOrEmpty(sorts))
        //        sorts = $" ORDER BY {sorts}";
        //}

        //var query = $"SELECT {columns} FROM {table}{joins}{filters}{groupBy}{sorts};";
        //return query;
    }

    public override string ToString()
    {
        return GetSql();
    }
}