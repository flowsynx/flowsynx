using System.Runtime.Serialization;
using System.Text;
using FlowSynx.Connectors.Database.MySql.Extensions;

namespace FlowSynx.Connectors.Database.MySql.Models;

/// <summary>
/// Inspired by SqlBuilder open source project (https://github.com/koshovyi/SqlBuilder/tree/master)
/// </summary>
public class Join
{
    public JoinType Type { get; set; }
    public required string Table { get; set; }
    public string? TableAlias { get; set; }
    public List<JoinItem> Expressions { get; set; } = new List<JoinItem>();

    public string GetSql(ISqlFormat format, string sourceTable)
    {
        var sb = new StringBuilder();
        sb.Append(GetJoinType());
        sb.Append(' ');
        sb.Append(SqlBuilder.FormatTable(format, Table));
        if (!string.IsNullOrEmpty(TableAlias))
        {
            sb.Append(format.AliasOperator);
            sb.Append(SqlBuilder.FormatTableAlias(format, TableAlias));
        }
        sb.Append(" ON ");

        var ex = new StringBuilder();
        foreach (var item in Expressions)
        {
            if (ex.Length > 0)
                ex.Append(" AND ");
            else
            {
                ex.Append(SqlBuilder.FormatTableAlias(format, sourceTable));
                ex.Append('.');
                ex.Append(SqlBuilder.FormatColumn(format, item.Name));
                ex.Append('=');
                ex.Append(string.IsNullOrEmpty(TableAlias)
                    ? SqlBuilder.FormatTable(format, Table)
                    : SqlBuilder.FormatTableAlias(format, TableAlias));
                ex.Append('.');
                ex.Append(SqlBuilder.FormatColumn(format, item.Value));
            }
        }
        sb.Append(ex);
        return sb.ToString();
    }

    private string GetJoinType()
    {
        switch (Type)
        {
            case JoinType.Right:
                return "RIGHT JOIN";
            case JoinType.Left:
                return "LEFT JOIN";
            case JoinType.Full:
                return "FULL JOIN";
            case JoinType.Inner:
            default:
                return "INNER JOIN";
        }
    }
}