using FlowSynx.Connectors.Database.MySql.Models;

namespace FlowSynx.Connectors.Database.MySql.Extensions;

public class EnumToQuery
{
    public static string OperatorsToSqlOperator(ComparisonOperator @operator)
    {
        return @operator switch
        {
            ComparisonOperator.Equals => "=",
            ComparisonOperator.GreaterThan => ">",
            ComparisonOperator.LessThan => "<",
            ComparisonOperator.GreaterOrEqual => ">=",
            ComparisonOperator.LessOrEqual => "<=",
            ComparisonOperator.NotEqual => "<>",
            ComparisonOperator.Like => "like",
            ComparisonOperator.NotLike => "not like",
            ComparisonOperator.In => "in",
            ComparisonOperator.NotIn => "not in",
            ComparisonOperator.Between => "between",
            ComparisonOperator.NotBetween => "not between",
            _ => "="
        };
    }
}
