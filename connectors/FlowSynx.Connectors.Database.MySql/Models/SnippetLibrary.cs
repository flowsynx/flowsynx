using FlowSynx.Connectors.Database.MySql.Extensions;

namespace FlowSynx.Connectors.Database.MySql.Models;

/// <summary>
/// Inspired by SqlBuilder open source project (https://github.com/koshovyi/SqlBuilder/tree/master)
/// </summary>
public static class SnippetLibrary
{
    public static Snippet End(string value)
    {
        return new Snippet("END", value);
    }
    
    public static Snippet Table(ISqlFormat format, string table, string? tableAlias = "")
    {
        table = SqlBuilder.FormatTable(format, table);
        if (!string.IsNullOrEmpty(tableAlias))
            tableAlias = SqlBuilder.FormatTableAlias(format, tableAlias);

        return string.IsNullOrEmpty(tableAlias) 
            ? new Snippet("TABLE", table) 
            : new Snippet("TABLE", table + format.AliasOperator + tableAlias);
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