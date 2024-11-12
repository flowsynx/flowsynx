using FlowSynx.Connectors.Database.MySql.Extensions;
using System.Text;

namespace FlowSynx.Connectors.Database.MySql.Models;

public class SortList: List<Sort>
{
    public MySqlFormat Parameters = new MySqlFormat();

    public string GetSql(string? tableAlias = "")
    {
        var sb = new StringBuilder();

        var sep = false;
        foreach (var sort in this)
        {
            if (sep)
                sb.Append(", ");
            else
                sep = true;
            sb.Append(SqlBuilder.FormatColumn(sort.Name, Parameters, tableAlias) + " " + sort.GetDirection());
        }

        return sb.ToString();
    }
}
