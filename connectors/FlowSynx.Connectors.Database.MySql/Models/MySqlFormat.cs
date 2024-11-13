namespace FlowSynx.Connectors.Database.MySql.Models;

public class MySqlFormat: ISqlFormat
{
    public char Parameter => '?';
    public bool EscapeEnabled => true;
    public char ColumnEscapeLeft => '`';
    public char ColumnEscapeRight => '`';
    public char TableEscapeLeft => '`';
    public char TableEscapeRight => '`';
    public char EndOfStatement => ';';
    public char AliasEscape => '\"';
    public string AliasOperator => " as ";
}