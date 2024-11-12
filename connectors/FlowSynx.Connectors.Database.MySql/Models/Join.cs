using System.Text;
using FlowSynx.Connectors.Database.MySql.Extensions;

namespace FlowSynx.Connectors.Database.MySql.Models;

public class Join
{
    public JoinType Type { get; set; }
    public required string Table { get; set; }
    public string? TableAlias { get; set; }
    public List<JoinItem> Expressions { get; set; } = new List<JoinItem>();

    public string GetSql(string sourceTable)
    {
        var sb = new StringBuilder();
        sb.Append(GetJoinType());
        sb.Append(' ');
        sb.Append(SqlBuilder.FormatTable(Table));
        if (!string.IsNullOrEmpty(TableAlias))
        {
            sb.Append(MySqlFormat.AliasOperator);
            sb.Append(SqlBuilder.FormatTableAlias(TableAlias));
        }
        sb.Append(" ON ");

        var ex = new StringBuilder();
        foreach (var item in Expressions)
        {
            if (ex.Length > 0)
                ex.Append(" AND ");
            else
            {
                ex.Append(SqlBuilder.FormatTableAlias(sourceTable));
                ex.Append('.');
                ex.Append(SqlBuilder.FormatColumn(item.Name));
                ex.Append('=');
                ex.Append(string.IsNullOrEmpty(TableAlias)
                    ? SqlBuilder.FormatTable(Table)
                    : SqlBuilder.FormatTableAlias(TableAlias));
                ex.Append('.');
                ex.Append(SqlBuilder.FormatColumn(item.Value));
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