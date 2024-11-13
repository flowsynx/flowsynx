using FlowSynx.Connectors.Database.MySql.Extensions;
using System.Text;

namespace FlowSynx.Connectors.Database.MySql.Models;

/// <summary>
/// Inspired by SqlBuilder open source project (https://github.com/koshovyi/SqlBuilder/tree/master)
/// </summary>
public class Field
{
    public required string Name { get; set; }
    public string? Alias { get; set; }

    public string GetSql(ISqlFormat format, string? tableAlias = "")
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(tableAlias))
            sb.Append(SqlBuilder.FormatTableAlias(format, tableAlias) + '.');

        sb.Append(SqlBuilder.FormatColumn(format, Name));

        if (!string.IsNullOrEmpty(Alias))
        {
            sb.Append(format.AliasOperator);
            sb.Append(format.AliasEscape);
            sb.Append(Alias);
            sb.Append(format.AliasEscape);
        }

        return sb.ToString();
    }
}
