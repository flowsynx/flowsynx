using System.Text;
using FlowSynx.Connectors.Database.MySql.Extensions;

namespace FlowSynx.Connectors.Database.MySql.Models;

public class FieldsList: List<Field>
{
    public string GetSql(string? tableAlias = "")
    {
        if (Count == 0)
        {
            return string.IsNullOrEmpty(tableAlias)
                ? "*"
                : SqlBuilder.FormatTableAlias(tableAlias) + ".*";
        }

        var sb = new StringBuilder();
        foreach (var field in this)
        {
            if (sb.Length > 0)
                sb.Append(", ");

            sb.Append(field.GetSql(tableAlias));
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        return GetSql();
    }
}