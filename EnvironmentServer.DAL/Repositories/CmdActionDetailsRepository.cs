using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using System.Dynamic;

namespace EnvironmentServer.DAL.Repositories;

public class CmdActionDetailsRepository
{
    private Database DB;

    public CmdActionDetailsRepository(Database db)
    {
        DB = db;
    }

    public long Create(string JsonString)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.QuerySingle<int>("Insert into `cmd_actions_details` (ID, JsonString) values " +
            "(NULL, @JsonString); SELECT LAST_INSERT_ID();", new
        {
            JsonString
        });
    }

    public CmdActionDetails Get(long id)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.QuerySingle<CmdActionDetails>("select * from `cmd_actions_details` where ID = @id", new
        {
            id
        });
    }

    public void Delete(long id)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("delete from `cmd_actions_details` where ID = @id", new
        {
            id
        });
    }
}