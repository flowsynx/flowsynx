using FlowSynx.Connectors.Database.MySql.Extensions;

namespace FlowSynx.Connectors.Database.MySql.Models;

public static class SnippetLibrary
{
    public static Snippet End(string value)
    {
        return new Snippet("END", value);
    }
    
    public static Snippet Table(string table, string? tableAlias = "")
    {
        table = SqlBuilder.FormatTable(table);
        if (!string.IsNullOrEmpty(tableAlias))
            tableAlias = SqlBuilder.FormatTableAlias(tableAlias);

        return string.IsNullOrEmpty(tableAlias) 
            ? new Snippet("TABLE", table) 
            : new Snippet("TABLE", table + MySqlFormat.AliasOperator + tableAlias);
    }

    public static Snippet Fields(string value)
    {
        return new Snippet("FIELDS", value);
    }

    public static Snippet Join(string value)
    {
        return new Snippet("JOINS", ' ' + value);
    }

    public static Snippet Filters(string value)
    {
        return new Snippet("FILTERS", value, " WHERE ");
    }

    public static Snippet Sort(string value)
    {
        return new Snippet("ORDERBY", value, " ORDER BY ");
    }

    public static Snippet GroupBy(string value)
    {
        return new Snippet("GROUPBY", value, " GROUP BY ");
    }
}