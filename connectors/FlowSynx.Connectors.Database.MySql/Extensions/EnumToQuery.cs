using FlowSynx.Connectors.Database.MySql.Models;

namespace FlowSynx.Connectors.Database.MySql.Extensions;

public class EnumToQuery
{
    public static string OperatorsToSqlOperator(Operator @operator)
    {
        return @operator switch
        {
            Operator.Equals => "=",
            Operator.GreaterThan => ">",
            Operator.LessThan => "<",
            Operator.GreaterOrEqual => ">=",
            Operator.LessOrEqual => "<=",
            Operator.NotEqual => "<>",
            Operator.Like => "like",
            Operator.NotLike => "not like",
            Operator.In => "in",
            Operator.NotIn => "not in",
            Operator.Between => "between",
            Operator.NotBetween => "not between",
            _ => "="
        };
    }
}
