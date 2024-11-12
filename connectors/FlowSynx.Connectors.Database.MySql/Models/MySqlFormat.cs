namespace FlowSynx.Connectors.Database.MySql.Models;

public static class MySqlFormat
{
    public static char Parameter => '?';
    public static bool EscapeEnabled => true;
    public static char ColumnEscapeLeft => '`';
    public static char ColumnEscapeRight => '`';
    public static char TableEscapeLeft => '`';
    public static char TableEscapeRight => '`';
    public static char EndOfStatement => ';';
    public static char AliasEscape => '\"';
    public static string AliasOperator => " as ";
}