using FlowSynx.Connectors.Database.MySql.Extensions;

namespace FlowSynx.Connectors.Database.MySql.Models;

public class Table
{
    public required string Name { get; set; }
    public string? Alias { get; set; }
    public MySqlFormat Parameters = new MySqlFormat();

    public string GetSql()
    {
        var tableAlias = "";
        var table = SqlBuilder.FormatTable(Name, Parameters);
        if (!string.IsNullOrEmpty(Alias))
            tableAlias = SqlBuilder.FormatTableAlias(Alias, Parameters);

        if (string.IsNullOrEmpty(tableAlias))
            return table;

        return table + Parameters.AliasOperator + tableAlias;
    }

    public override string ToString()
    {
        return GetSql();
    }
}
