using FlowSynx.Connectors.Database.MySql.Extensions;
using System.Linq.Expressions;
using System.Text;

namespace FlowSynx.Connectors.Database.MySql.Models;

public class GroupBy
{
    public required string Name { get; set; }

    public string GetSql(string? tableAlias = "")
    {
        var sb = new StringBuilder();
        sb.Append(SqlBuilder.FormatColumn(Name, tableAlias));
        return sb.ToString();
    }
}