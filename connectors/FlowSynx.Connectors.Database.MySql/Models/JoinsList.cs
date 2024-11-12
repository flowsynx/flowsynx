using System.Text;

namespace FlowSynx.Connectors.Database.MySql.Models;

public class JoinsList: List<Join>
{
    public string GetSql(string sourceTable)
    {
        var sb = new StringBuilder();
        foreach (var join in this)
        {
            if (sb.Length > 0)
                sb.Append(' ');
            sb.Append(join.GetSql(sourceTable));
        }
        return sb.ToString();
    }
}