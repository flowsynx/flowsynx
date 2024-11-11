using FlowSynx.Connectors.Database.MySql.Models;

namespace FlowSynx.Connectors.Database.MySql.Extensions;

public static class SqlBuilder
{
    public static string FormatColumn(string column, MySqlFormat formatter, string tableAlias = "")
    {
        if (!string.IsNullOrEmpty(tableAlias))
            tableAlias = FormatTableAlias(tableAlias, formatter) + '.';

        column = formatter.EscapeEnabled
            ? formatter.ColumnEscapeLeft + column + formatter.ColumnEscapeRight
            : column;

        return tableAlias + column;
    }

    public static string FormatTable(string tableName, MySqlFormat formatter)
    {
        return formatter.EscapeEnabled
            ? formatter.TableEscapeLeft + tableName + formatter.TableEscapeRight
            : tableName;
    }

    public static string FormatTableAlias(string value, MySqlFormat formatter)
    {
        return formatter.TableEscapeLeft + value + formatter.TableEscapeRight;
    }
}