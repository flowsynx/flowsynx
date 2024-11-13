using FlowSynx.Connectors.Database.MySql.Models;

namespace FlowSynx.Connectors.Database.MySql.Extensions;

public static class SqlBuilder
{
    public static string FormatColumn(ISqlFormat format, string column, string? tableAlias = "")
    {
        if (!string.IsNullOrEmpty(tableAlias))
            tableAlias = FormatTableAlias(format, tableAlias) + '.';

        column = format.EscapeEnabled
            ? format.ColumnEscapeLeft + column + format.ColumnEscapeRight
            : column;

        return tableAlias + column;
    }

    public static string FormatTable(ISqlFormat format, string tableName)
    {
        return format.EscapeEnabled
            ? format.TableEscapeLeft + tableName + format.TableEscapeRight
            : tableName;
    }

    public static string FormatTableAlias(ISqlFormat format, string value)
    {
        return format.TableEscapeLeft + value + format.TableEscapeRight;
    }
}