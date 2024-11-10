using FlowSynx.Connectors.Database.MySql.Extensions;
using SqlKata;

namespace FlowSynx.Connectors.Database.MySql.Models;

public class Filter
{
    public Operator Operator { get; set; }
    public required string Name { get; set; }
    public required string Value { get; set; }
    public List<Filter>? Or { get; set; } = new List<Filter>();

    public override string ToString()
    {
        return base.ToString();
    }

    private void ParseWheres(List<Filter> filters, Query query)
    {
        if (!filters.Any()) return;
        foreach (var filter in filters)
        {
            query.Where(wrap =>
            {
                switch (filter.Operator)
                {
                    case Operator.In:
                        wrap.WhereIn(filter.Name, filter.Value.ToString()?.Split(','));
                        break;
                    case Operator.NotIn:
                        wrap.WhereNotIn(filter.Name, filter.Value.ToString()?.Split(','));
                        break;
                    case Operator.Like:
                        wrap.WhereLike(filter.Name, filter.Value);
                        break;
                    case Operator.NotLike:
                        wrap.WhereNotLike(filter.Name, filter.Value);
                        break;
                    //case Operator.Between:
                    //    wrap.WhereBetween(filter.Name, filter.Value, where.ColumnValueMax);
                    //    break;
                    //case Operator.NotBetween:
                    //    wrap.WhereNotBetween(filter.Name, filter.Value, where.ColumnValueMax);
                    //    break;
                    default:
                        wrap.Where(filter.Name,
                            EnumToQuery.OperatorsToSqlOperator(filter.Operator), filter.Value);
                        break;
                }

                ParseOrClauses(filter.Or, wrap);
                return wrap;
            });
        }
    }

    private void ParseOrClauses(List<Filter> filters, Query query)
    {
        if (!filters.Any()) return;
        foreach (var where in filters)
        {
            switch (where.Operator)
            {
                case Operator.In:
                    query.OrWhereIn(where.Name, where.Value.ToString()?.Split(','));
                    break;
                case Operator.NotIn:
                    query.OrWhereNotIn(where.Name, where.Value.ToString()?.Split(','));
                    break;
                case Operator.Like:
                    query.OrWhereLike(where.Name, where.Value);
                    break;
                case Operator.NotLike:
                    query.OrWhereNotLike(where.Name, where.Value);
                    break;
                //case Operator.Between:
                //    query.OrWhereBetween(where.Name, where.Value, where.ColumnValueMax);
                //    break;
                //case Operator.NotBetween:
                //    query.OrWhereNotBetween(where.Name, where.Value, where.ColumnValueMax);
                //    break;
                default:
                    query.OrWhere(where.Name, EnumToQuery.OperatorsToSqlOperator(where.Operator), where.Value);
                    break;
            }
        }
    }
}