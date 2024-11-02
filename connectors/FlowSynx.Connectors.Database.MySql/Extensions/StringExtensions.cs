namespace FlowSynx.Connectors.Database.MySql.Extensions;

public static class StringExtensions
{
    public static bool IsCreateDatabaseStatement(this string statement)
    {
        if (string.IsNullOrWhiteSpace(statement)) 
            return false;

        return statement.Contains("CREATE DATABASE", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsInsertStatement(this string statement)
    {
        if (string.IsNullOrWhiteSpace(statement))
            return false;

        return statement.Contains("INSERT INTO", StringComparison.OrdinalIgnoreCase);
    }
}