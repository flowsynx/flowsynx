using FlowSynx.Connectors.Database.MySql.Extensions;
using System.Text;

namespace FlowSynx.Connectors.Database.MySql.Models;

public class SortsList: List<Sort>
{
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

            sb.Append(sort.GetSql(tableAlias));
        }

        return sb.ToString();
    }
}
