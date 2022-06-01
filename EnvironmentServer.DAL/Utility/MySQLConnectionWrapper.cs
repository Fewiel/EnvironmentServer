using MySql.Data.MySqlClient;
using System;

namespace EnvironmentServer.DAL.Utility;

public class MySQLConnectionWrapper : IDisposable
{
    public MySqlConnection Connection { get; }

    public MySQLConnectionWrapper(string connString)
    {
        Connection = new MySqlConnection(connString);
        Connection.Open();
    }

    public void Dispose()
    {
        Connection.Close();
        Connection.Dispose();

        GC.SuppressFinalize(this);
    }
}