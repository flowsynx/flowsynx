namespace FlowSynx.Connectors.Database.MySql.Models;

internal class MySqlEntityPart
{
    public MySqlEntityPart(string databaseName, string tableName, string relativeEntity)
    {
        DatabaseName = databaseName;
        TableName = tableName;
        RelativeEntity = relativeEntity;
    }

    public string DatabaseName { get; }
    public string TableName { get; }
    public string RelativeEntity { get; }
}