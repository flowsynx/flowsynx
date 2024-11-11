using System.Text;
using FlowSynx.Connectors.Database.MySql.Extensions;

namespace FlowSynx.Connectors.Database.MySql.Models;

public class FieldsList: List<Field>
{
    public MySqlFormat Parameters = new MySqlFormat();

    public string GetSql(string? tableAlias = "")
    {
        if (Count == 0)
        {
            return string.IsNullOrEmpty(tableAlias)
                ? "*"
                : SqlBuilder.FormatTableAlias(tableAlias, Parameters) + ".*";
        }

        var sb = new StringBuilder();
        foreach (var field in this)
        {
            if (sb.Length > 0)
                sb.Append(", ");

            if (!string.IsNullOrEmpty(tableAlias))
                sb.Append(SqlBuilder.FormatTableAlias(tableAlias, Parameters) + '.');
            
            sb.Append(SqlBuilder.FormatColumn(field.Name, Parameters));

            if (!string.IsNullOrEmpty(field.Alias))
            {
                sb.Append(Parameters.AliasOperator);
                sb.Append(Parameters.AliasEscape);
                sb.Append(field.Alias);
                sb.Append(Parameters.AliasEscape);
            }
        }
        return sb.ToString();
    }

    public override string ToString()
    {
        return GetSql();
    }
}