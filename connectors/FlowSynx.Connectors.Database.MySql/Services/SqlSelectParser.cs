using FlowSynx.Connectors.Database.MySql.Models;

namespace FlowSynx.Connectors.Database.MySql.Services;

public class SqlSelectParser
{
    public string GetSql(QueryData queryData)
    {
        var columns = "*";
        if (queryData.Fields != null && queryData.Fields.Any())
            columns = string.Join(", ", queryData.Fields.Select(x=>x.ToString()));

        var query = $"SELECT {columns} FROM {queryData.Table};";
        return query;
    }
}