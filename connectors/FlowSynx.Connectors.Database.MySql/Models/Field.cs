using FlowSynx.Connectors.Database.MySql.Extensions;
using System.Text;

namespace FlowSynx.Connectors.Database.MySql.Models;

public class Field
{
    public required string Name { get; set; }
    public string? Alias { get; set; }

    public string GetSql(string? tableAlias = "")
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(tableAlias))
            sb.Append(SqlBuilder.FormatTableAlias(tableAlias) + '.');

        sb.Append(SqlBuilder.FormatColumn(Name));

        if (!string.IsNullOrEmpty(Alias))
        {
            sb.Append(MySqlFormat.AliasOperator);
            sb.Append(MySqlFormat.AliasEscape);
            sb.Append(Alias);
            sb.Append(MySqlFormat.AliasEscape);
        }

        return sb.ToString();
    }
}
