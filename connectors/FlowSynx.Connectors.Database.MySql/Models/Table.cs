using FlowSynx.Connectors.Database.MySql.Extensions;

namespace FlowSynx.Connectors.Database.MySql.Models;

public class Table
{
    public required string Name { get; set; }
    public string? Alias { get; set; }

    public string GetSql()
    {
        var tableAlias = "";
        var table = SqlBuilder.FormatTable(Name);
        if (!string.IsNullOrEmpty(Alias))
            tableAlias = SqlBuilder.FormatTableAlias(Alias);

        return string.IsNullOrEmpty(tableAlias) 
            ? table 
            : $"{table}{MySqlFormat.AliasOperator}{tableAlias}";
    }

    public override string ToString()
    {
        return GetSql();
    }
}
