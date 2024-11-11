namespace FlowSynx.Connectors.Database.MySql.Models;

public enum FilterType
{
    Equals,
    GreaterThan,
    LessThan,
    GreaterOrEqual,
    LessOrEqual,
    NotEqual,
    Like,
    NotLike,
    IsNull,
    IsNotNull,
    In,
    NotIn,
    Between,
    NotBetween
}