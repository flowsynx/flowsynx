using FlowSynx.Connectors.Database.MySql.Models;
using MySql.Data.MySqlClient;

namespace FlowSynx.Connectors.Database.MySql.Services;

public interface IMySqlConnection
{
    MySqlConnection Connect(MySqlpecifications specifications);
}