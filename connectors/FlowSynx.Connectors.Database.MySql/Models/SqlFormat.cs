namespace FlowSynx.Connectors.Database.MySql.Models;

/// <summary>
/// Inspired by SqlBuilder open source project (https://github.com/koshovyi/SqlBuilder/tree/master)
/// </summary>
public interface ISqlFormat
{
    char Parameter { get; }
    bool EscapeEnabled { get; }
    char ColumnEscapeLeft { get; }
    char ColumnEscapeRight { get; }
    char TableEscapeLeft { get; }
    char TableEscapeRight { get; }
    char EndOfStatement { get; }
    char AliasEscape { get; }
    string AliasOperator { get; }
}