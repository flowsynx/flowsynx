using FlowSynx.Connectors.Database.MySql.Extensions;

namespace FlowSynx.Connectors.Database.MySql.Models;

public class Table
{
    public required string Name { get; set; }
    public string? Alias { get; set; }

    public string GetSql(ISqlFormat format)
    {
        var tableAlias = "";
        var table = SqlBuilder.FormatTable(format, Name);
        if (!string.IsNullOrEmpty(Alias))
            tableAlias = SqlBuilder.FormatTableAlias(format, Alias);

        return string.IsNullOrEmpty(tableAlias) 
            ? table 
            : $"{table}{format.AliasOperator}{tableAlias}";
    }
}
