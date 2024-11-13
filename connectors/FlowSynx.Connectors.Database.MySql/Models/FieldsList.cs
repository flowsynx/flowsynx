using System.Text;
using FlowSynx.Connectors.Database.MySql.Extensions;

namespace FlowSynx.Connectors.Database.MySql.Models;

/// <summary>
/// Inspired by SqlBuilder open source project (https://github.com/koshovyi/SqlBuilder/tree/master)
/// </summary>
public class FieldsList: List<Field>
{
    public string GetSql(ISqlFormat format, string? tableAlias = "")
    {
        if (Count == 0)
        {
            return string.IsNullOrEmpty(tableAlias)
                ? "*"
                : SqlBuilder.FormatTableAlias(format, tableAlias) + ".*";
        }

        var sb = new StringBuilder();
        foreach (var field in this)
        {
            if (sb.Length > 0)
                sb.Append(", ");

            sb.Append(field.GetSql(format, tableAlias));
        }

        return sb.ToString();
    }
}