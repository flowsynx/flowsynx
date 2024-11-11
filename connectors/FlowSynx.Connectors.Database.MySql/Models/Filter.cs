using System.Text;

namespace FlowSynx.Connectors.Database.MySql.Models;

public class Filter
{
    public LogicOperator? Operator { get; set; } = LogicOperator.And;
    public ComparisonOperator Comparison { get; set; }
    public required string Name { get; set; }
    public required string Value { get; set; }
    public string? ValueMax { get; set; }
    public List<Filter>? Filters { get; set; } = new List<Filter>();

    public override string ToString()
    {
        return base.ToString();
    }

    private string GetLogicOperator(LogicOperator logicOperator)
    {
        switch (logicOperator)
        {
            case LogicOperator.AndNot:
                return "AND NOT";
            case LogicOperator.Or:
                return "OR";
            default:
            case LogicOperator.And:
                return "AND";
        }
    }

    private string In(string name, params string[] rawSql)
    {
        StringBuilder sb = new StringBuilder();
        foreach (string expression in rawSql)
        {
            if (sb.Length > 0)
                sb.Append(", ");
            sb.Append(expression);
        }

        return $"{name} IN (" + sb.ToString() + ")";
    }

    private string EqualValue(string name, string value)
    {
        return $"{name}={value}";
    }

    private string NotEqualValue(string name, string value)
    {
        return $"{name}!={value}";
    }

    private string EqualLessValue(string name, string value)
    {
        return $"{name}<={value}";
    }

    private string EqualGreaterValue(string name, string value)
    {
        return $"{name}>={value}";
    }

    private string LessValue(string name, string value)
    {
        return $"{name}<{value}";
    }

    private string GreaterValue(string name, string value)
    {
        return $"{name}>{value}";
    }

    private string IsNull(string name)
    {
        return $"{name} IS NULL";
    }

    private string IsNotNull(string name)
    {
        return $"{name} IS NOT NULL";
    }

    private string Between(string name, string begin, string end)
    {
        return $" BETWEEN {begin} AND {end}";
    }

    private string NotBetween(string name, string begin, string end)
    {
        return $" NOT BETWEEN {begin} AND {end}";
    }

    private string Like(string name, string value)
    {
        return $"{name} LIKE {value}";
    }

    private string NotLike(string name, string value)
    {
        return $"{name} NOT LIKE {value}";
    }
}