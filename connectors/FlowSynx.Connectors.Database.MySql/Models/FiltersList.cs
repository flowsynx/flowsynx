using System.Text;

namespace FlowSynx.Connectors.Database.MySql.Models;

/// <summary>
/// Inspired by SqlBuilder open source project (https://github.com/koshovyi/SqlBuilder/tree/master)
/// </summary>
public class FiltersList: List<Filter>
{
    private string GetLogicOperator(LogicOperator? filterOperator)
    {
        switch (filterOperator)
        {
            case LogicOperator.AndNot:
                return "AND NOT";
            case LogicOperator.Or:
                return "OR";
            case LogicOperator.And:
            default:
                return "AND";
        }
    }
    
    public string GetSql(ISqlFormat format, string? tableAlias = "")
    {
        var sb = new StringBuilder();
        foreach (var filter in this)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
                sb.Append(GetLogicOperator(filter.Logic));
                sb.Append(' ');
            }

            sb.Append(filter.GetSql(format, tableAlias));
        }

        return sb.ToString();
    }
}