using FlowSynx.Connectors.Database.MySql.Models;
using MySql.Data.MySqlClient;

namespace FlowSynx.Connectors.Database.MySql.Services;

public class MySqlDatabaseConnection : IMySqlDatabaseConnection
{
    public MySqlConnection Connect(MySqlpecifications specifications)
    {
        var connection = new MySqlConnection(specifications.Url);
        connection.Open();

        return connection;
    }
}