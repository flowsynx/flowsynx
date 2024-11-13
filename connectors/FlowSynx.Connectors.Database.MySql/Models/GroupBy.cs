using FlowSynx.Connectors.Database.MySql.Extensions;
using System.Linq.Expressions;
using System.Text;

namespace FlowSynx.Connectors.Database.MySql.Models;

/// <summary>
/// Inspired by SqlBuilder open source project (https://github.com/koshovyi/SqlBuilder/tree/master)
/// </summary>
public class GroupBy
{
    public required string Name { get; set; }

    public string GetSql(ISqlFormat format, string? tableAlias = "")
    {
        var sb = new StringBuilder();
        sb.Append(SqlBuilder.FormatColumn(format, Name, tableAlias));
        return sb.ToString();
    }
}