using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using System.Collections.Generic;
using System.Linq;

namespace EnvironmentServer.DAL.Repositories;

public class PerformanceRepository
{
    private Database DB;

    public PerformanceRepository(Database db)
    {
        DB = db;
    }

    public Dictionary<string, string> Get()
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        var result = new Dictionary<string, string>();
        foreach(var kv in c.Connection.Query<PerformanceMetric>("select * from `performance`"))
            result.Add(kv.Name, kv.Value);

        return result;
    }

    public void Set(string name, string value)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("update `performance` set `value` = @value where `name` = @name", new
        {
            name,
            value
        });
    }

    public int GetUsers()
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.QuerySingle<int>("SELECT COUNT(*) FROM `users`;");
    }

    public int GetEnvironments()
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.QuerySingle<int>("SELECT COUNT(*) FROM `environments`;");
    }

    public int GetStoredEnvironments()
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.QuerySingle<int>("SELECT COUNT(*) FROM `environments` where `Stored` = 1;");
    }

    public int GetQueue()
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.QuerySingle<int>("SELECT COUNT(*) FROM `cmd_actions` where `Executed` is NULL;");
    }
}