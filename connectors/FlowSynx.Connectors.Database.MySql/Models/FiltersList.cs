using FlowSynx.Connectors.Database.MySql.Extensions;
using System.Text;

namespace FlowSynx.Connectors.Database.MySql.Models;

public class FiltersList: List<Filter>
{
    public MySqlFormat Parameters = new MySqlFormat();

    private string GetLogicOperator(LogicOperator? logicOperator)
    {
        switch (logicOperator)
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

    private string NotIn(string name, params string[] rawSql)
    {
        StringBuilder sb = new StringBuilder();
        foreach (string expression in rawSql)
        {
            if (sb.Length > 0)
                sb.Append(", ");
            sb.Append(expression);
        }

        return $"{name} NOT IN (" + sb.ToString() + ")";
    }

    private string EqualValue(string name, string? value, string? tableAlias = "")
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(tableAlias))
            sb.Append(SqlBuilder.FormatTableAlias(tableAlias, Parameters) + '.');

        sb.Append(SqlBuilder.FormatColumn(name, Parameters));
        sb.Append("=");
        sb.Append(value);
        
        return sb.ToString();
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
        return $"{name} LIKE '{value}'";
    }

    private string NotLike(string name, string value)
    {
        return $"{name} NOT LIKE '{value}'";
    }

    public string GetSql(string? tableAlias = "")
    {
        var sb = new StringBuilder();
        foreach (var filter in this)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
                sb.Append(GetLogicOperator(filter.Operator));
                sb.Append(' ');
            }

            sb.Append('(');
            switch (filter.Comparison)
            {
                case ComparisonOperator.Equals:
                    sb.Append(EqualValue(filter.Name, filter.Value, tableAlias));
                    break;
                case ComparisonOperator.GreaterOrEqual:
                    sb.Append(EqualGreaterValue(filter.Name, filter.Value));
                    break;
                case ComparisonOperator.GreaterThan:
                    sb.Append(GreaterValue(filter.Name, filter.Value));
                    break;
                case ComparisonOperator.LessOrEqual:
                    sb.Append(EqualLessValue(filter.Name, filter.Value));
                    break;
                case ComparisonOperator.LessThan:
                    sb.Append(LessValue(filter.Name, filter.Value));
                    break;
                case ComparisonOperator.NotEqual:
                    sb.Append(NotEqualValue(filter.Name, filter.Value));
                    break;
                case ComparisonOperator.Like:
                    sb.Append(Like(filter.Name, filter.Value));
                    break;
                case ComparisonOperator.NotLike:
                    sb.Append(NotLike(filter.Name, filter.Value));
                    break;
                case ComparisonOperator.In:
                    sb.Append(In(filter.Name, filter.Value));
                    break;
                case ComparisonOperator.NotIn:
                    sb.Append(NotIn(filter.Name, filter.Value));
                    break;
                case ComparisonOperator.IsNull:
                    sb.Append(IsNull(filter.Name));
                    break;
                case ComparisonOperator.IsNotNull:
                    sb.Append(IsNotNull(filter.Name));
                    break;
                case ComparisonOperator.Between:
                    sb.Append(Between(filter.Name, filter.Value, filter.ValueMax));
                    break;
                case ComparisonOperator.NotBetween:
                    sb.Append(NotBetween(filter.Name, filter.Value, filter.ValueMax));
                    break;
            }
            sb.Append(')');
        }

        return sb.ToString();
    }

}