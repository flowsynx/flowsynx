using System.Text;

namespace FlowSynx.Connectors.Database.MySql.Models;

/// <summary>
/// Inspired by SqlBuilder open source project (https://github.com/koshovyi/SqlBuilder/tree/master)
/// </summary>
public class JoinsList: List<Join>
{
    public string GetSql(ISqlFormat format, string sourceTable)
    {
        var sb = new StringBuilder();
        foreach (var join in this)
        {
            if (sb.Length > 0)
                sb.Append(' ');
            sb.Append(join.GetSql(format, sourceTable));
        }
        return sb.ToString();
    }
}