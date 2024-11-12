using FlowSynx.Connectors.Database.MySql.Models;

namespace FlowSynx.Connectors.Database.MySql.Extensions;

public static class SqlBuilder
{
    public static string FormatColumn(string column, string? tableAlias = "")
    {
        if (!string.IsNullOrEmpty(tableAlias))
            tableAlias = FormatTableAlias(tableAlias) + '.';

        column = MySqlFormat.EscapeEnabled
            ? MySqlFormat.ColumnEscapeLeft + column + MySqlFormat.ColumnEscapeRight
            : column;

        return tableAlias + column;
    }

    public static string FormatTable(string tableName)
    {
        return MySqlFormat.EscapeEnabled
            ? MySqlFormat.TableEscapeLeft + tableName + MySqlFormat.TableEscapeRight
            : tableName;
    }

    public static string FormatTableAlias(string value)
    {
        return MySqlFormat.TableEscapeLeft + value + MySqlFormat.TableEscapeRight;
    }
}