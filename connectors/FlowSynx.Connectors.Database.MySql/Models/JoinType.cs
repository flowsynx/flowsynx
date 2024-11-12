namespace FlowSynx.Connectors.Database.MySql.Models;

public enum JoinType : uint
{
    Inner = 1,
    Left  = 2,
    Right = 3,
    Full  = 4,
}