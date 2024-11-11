using FlowSynx.Connectors.Database.MySql.Extensions;
using System.Text;

namespace FlowSynx.Connectors.Database.MySql.Models;

public class FiltersList: List<Filter>
{
    public MySqlFormat Parameters = new MySqlFormat();

    private string GetFilterOperator(FilterOperator? filterOperator)
    {
        switch (filterOperator)
        {
            case FilterOperator.AndNot:
                return "AND NOT";
            case FilterOperator.Or:
                return "OR";
            case FilterOperator.And:
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

    private string EqualValue(string name, string? value)
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

    private string Between(string name, string begin, string? end)
    {
        return $" BETWEEN {begin} AND {end}";
    }

    private string NotBetween(string name, string begin, string? end)
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

    private string GetFieldName(string name, string? tableAlias = "")
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(tableAlias))
            sb.Append(SqlBuilder.FormatTableAlias(tableAlias, Parameters) + '.');

        sb.Append(SqlBuilder.FormatColumn(name, Parameters));

        return sb.ToString();
    }

    public string GetSql(string? tableAlias = "")
    {
        var sb = new StringBuilder();
        foreach (var filter in this)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
                sb.Append(GetFilterOperator(filter.Operator));
                sb.Append(' ');
            }

            sb.Append('(');

            var fieldName = GetFieldName(filter.Name, tableAlias);

            switch (filter.Type)
            {
                case FilterType.Equals:
                    sb.Append(EqualValue(fieldName, filter.Value));
                    break;
                case FilterType.GreaterOrEqual:
                    sb.Append(EqualGreaterValue(fieldName, filter.Value));
                    break;
                case FilterType.GreaterThan:
                    sb.Append(GreaterValue(fieldName, filter.Value));
                    break;
                case FilterType.LessOrEqual:
                    sb.Append(EqualLessValue(fieldName, filter.Value));
                    break;
                case FilterType.LessThan:
                    sb.Append(LessValue(fieldName, filter.Value));
                    break;
                case FilterType.NotEqual:
                    sb.Append(NotEqualValue(fieldName, filter.Value));
                    break;
                case FilterType.Like:
                    sb.Append(Like(fieldName, filter.Value));
                    break;
                case FilterType.NotLike:
                    sb.Append(NotLike(fieldName, filter.Value));
                    break;
                case FilterType.In:
                    sb.Append(In(fieldName, filter.Value));
                    break;
                case FilterType.NotIn:
                    sb.Append(NotIn(fieldName, filter.Value));
                    break;
                case FilterType.IsNull:
                    sb.Append(IsNull(fieldName));
                    break;
                case FilterType.IsNotNull:
                    sb.Append(IsNotNull(fieldName));
                    break;
                case FilterType.Between:
                    sb.Append(Between(fieldName, filter.Value, filter.ValueMax));
                    break;
                case FilterType.NotBetween:
                    sb.Append(NotBetween(fieldName, filter.Value, filter.ValueMax));
                    break;
            }
            sb.Append(')');
        }

        return sb.ToString();
    }

}