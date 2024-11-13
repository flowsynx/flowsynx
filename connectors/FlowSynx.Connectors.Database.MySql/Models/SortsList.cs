using System.Text;

namespace FlowSynx.Connectors.Database.MySql.Models;

/// <summary>
/// Inspired by SqlBuilder open source project (https://github.com/koshovyi/SqlBuilder/tree/master)
/// </summary>
public class SortsList: List<Sort>
{
    public string GetSql(ISqlFormat format, string? tableAlias = "")
    {
        var sb = new StringBuilder();

        var sep = false;
        foreach (var sort in this)
        {
            if (sep)
                sb.Append(", ");
            else
                sep = true;

            sb.Append(sort.GetSql(format, tableAlias));
        }

        return sb.ToString();
    }
}
