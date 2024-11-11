namespace FlowSynx.Connectors.Database.MySql.Models;

public class MySqlFormat
{
    public MySqlFormat()
    {
        EscapeEnabled = true;
        TableEscapeLeft = '`';
        TableEscapeRight = '`';
        ColumnEscapeLeft = '`';
        ColumnEscapeRight = '`';
        Parameter = '?';
        EndOfStatement = ';';
        AliasEscape = '\"';
        AliasOperator = " as ";
    }

    public char Parameter { get; set; }

    public bool EscapeEnabled { get; set; }

    public char ColumnEscapeLeft { get; set; }

    public char ColumnEscapeRight { get; set; }

    public char TableEscapeLeft { get; set; }

    public char TableEscapeRight { get; set; }

    public char EndOfStatement { get; set; }

    public char AliasEscape { get; set; }

    public string AliasOperator { get; set; }
}