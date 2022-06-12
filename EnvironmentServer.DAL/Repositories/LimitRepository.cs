using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using System.Collections.Generic;
using System.Dynamic;

namespace EnvironmentServer.DAL.Repositories;

public class LimitRepository
{
    private readonly Database DB;

    public LimitRepository(Database db)
    {
        DB = db;
    }

    public IEnumerable<Limit> GetAll()
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.Query<Limit>("select * from `limits`;");
    }

    public Limit Get(string internalName)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.QueryFirstOrDefault("select * from `limits` where `InternalName` = @internalName;", new
        {
            internalName
        });
    }

    public void Add(Limit l)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("insert into `limits` (`Name`, `InternalName`) values (@name, @internalName)", new
        {
            name = l.Name,
            internalName = l.InternalName
        });
    }

    public void Udpdate(Limit l)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("update `limits` set `Name` = @name, `InternalName` = @internalName where `ID` = @id", new
        {
            id = l.ID,
            name = l.Name,
            internalName = l.InternalName
        });
    }

    public void Delete(string internalName)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("delete from `limits` where `InternalName` = @internalName;", new
        {
            internalName
        });
    }
}