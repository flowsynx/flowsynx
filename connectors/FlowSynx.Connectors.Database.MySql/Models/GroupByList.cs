using System.Text;

namespace FlowSynx.Connectors.Database.MySql.Models;

public class GroupByList: List<GroupBy>
{
    public string GetSql(string? tableAlias = "")
    {
        var sb = new StringBuilder();

        var sep = false;
        foreach (var groupBy in this)
        {
            if (sep)
                sb.Append(", ");
            else
                sep = true;
            sb.Append(groupBy.GetSql(tableAlias));
        }

        return sb.ToString();
    }
}