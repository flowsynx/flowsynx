using FlowSynx.Connectors.Database.MySql.Extensions;
using Google.Protobuf.WellKnownTypes;
using System.Text;

namespace FlowSynx.Connectors.Database.MySql.Models;

public class Filter
{
    public LogicOperator? Logic { get; set; } = LogicOperator.And;
    public ComparisonOperator Comparison { get; set; }
    public required string Name { get; set; }
    public string? Value { get; set; }
    public string? ValueMax { get; set; }
    public List<Filter>? Filters { get; set; } = new List<Filter>();

    public string GetSql(string? tableAlias = "")
    {
        var sb = new StringBuilder();

        sb.Append('(');

        var fieldName = GetFieldName(Name, tableAlias);

        switch (Comparison)
        {
            case ComparisonOperator.Equals:
                sb.Append(EqualValue(fieldName, Value));
                break;
            case ComparisonOperator.GreaterOrEqual:
                sb.Append(EqualGreaterValue(fieldName, Value));
                break;
            case ComparisonOperator.GreaterThan:
                sb.Append(GreaterValue(fieldName, Value));
                break;
            case ComparisonOperator.LessOrEqual:
                sb.Append(EqualLessValue(fieldName, Value));
                break;
            case ComparisonOperator.LessThan:
                sb.Append(LessValue(fieldName, Value));
                break;
            case ComparisonOperator.NotEqual:
                sb.Append(NotEqualValue(fieldName, Value));
                break;
            case ComparisonOperator.Like:
                sb.Append(Like(fieldName, Value));
                break;
            case ComparisonOperator.NotLike:
                sb.Append(NotLike(fieldName, Value));
                break;
            case ComparisonOperator.In:
                sb.Append(In(fieldName, Value?.Split(',')));
                break;
            case ComparisonOperator.NotIn:
                sb.Append(NotIn(fieldName, Value?.Split(',')));
                break;
            case ComparisonOperator.IsNull:
                sb.Append(IsNull(fieldName));
                break;
            case ComparisonOperator.IsNotNull:
                sb.Append(IsNotNull(fieldName));
                break;
            case ComparisonOperator.Between:
                sb.Append(Between(fieldName, Value, ValueMax));
                break;
            case ComparisonOperator.NotBetween:
                sb.Append(NotBetween(fieldName, Value, ValueMax));
                break;
        }
        sb.Append(')');

        return sb.ToString();
    }

    private string GetFieldName(string name, string? tableAlias = "")
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(tableAlias))
            sb.Append(SqlBuilder.FormatTableAlias(tableAlias) + '.');

        sb.Append(SqlBuilder.FormatColumn(name));

        return sb.ToString();
    }

    private string EqualValue(string name, string? value)
    {
        if (string.IsNullOrEmpty(value))
            value = string.Empty;
        return $"{name}={value}";
    }

    private string EqualGreaterValue(string name, string? value)
    {
        if (string.IsNullOrEmpty(value))
            value = string.Empty;
        return $"{name}>={value}";
    }

    private string GreaterValue(string name, string? value)
    {
        if (string.IsNullOrEmpty(value))
            value = string.Empty;
        return $"{name}>{value}";
    }

    private string EqualLessValue(string name, string? value)
    {
        if (string.IsNullOrEmpty(value))
            value = string.Empty;
        return $"{name}<={value}";
    }

    private string LessValue(string name, string? value)
    {
        return $"{name}<{value}";
    }

    private string NotEqualValue(string name, string? value)
    {
        if (string.IsNullOrEmpty(value))
            value = string.Empty;
        return $"{name}!={value}";
    }

    private string Like(string name, string? value)
    {
        if (string.IsNullOrEmpty(value))
            value = string.Empty;
        return $"{name} LIKE '{value}'";
    }

    private string NotLike(string name, string? value)
    {
        if (string.IsNullOrEmpty(value))
            value = string.Empty;
        return $"{name} NOT LIKE '{value}'";
    }

    private string In(string name, params string[]? rawSql)
    {
        var sb = new StringBuilder();

        if (rawSql != null)
        {
            foreach (var expression in rawSql)
            {
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(expression);
            }
        }

        return $"{name} IN (" + sb + ")";
    }

    private string NotIn(string name, params string[]? rawSql)
    {
        var sb = new StringBuilder();

        if (rawSql != null)
        {
            foreach (var expression in rawSql)
            {
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(expression);
            }
        }

        return $"{name} NOT IN (" + sb + ")";
    }
    
    private string IsNull(string name)
    {
        return $"{name} IS NULL";
    }

    private string IsNotNull(string name)
    {
        return $"{name} IS NOT NULL";
    }

    private string Between(string name, string? begin, string? end)
    {
        if (string.IsNullOrEmpty(begin))
            begin = string.Empty;
        if (string.IsNullOrEmpty(end))
            end = string.Empty;
        return $" BETWEEN {begin} AND {end}";
    }

    private string NotBetween(string name, string? begin, string? end)
    {
        if (string.IsNullOrEmpty(begin))
            begin = string.Empty;
        if (string.IsNullOrEmpty(end))
            end = string.Empty;
        return $" NOT BETWEEN {begin} AND {end}";
    }
}