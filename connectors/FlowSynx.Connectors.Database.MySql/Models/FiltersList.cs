using System.Text;

namespace FlowSynx.Connectors.Database.MySql.Models;

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
    
    public string GetSql(string? tableAlias = "")
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

            sb.Append(filter.GetSql(tableAlias));
        }

        return sb.ToString();
    }
}