using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.InteropServices;

namespace EnvironmentServer.DAL.Repositories;

public class LimitRepository
{
    private readonly Database DB;
    private readonly Dictionary<string, Limit> LimitCache = new();

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
        if (LimitCache.TryGetValue(internalName, out var value))
            return value;

        using var c = new MySQLConnectionWrapper(DB.ConnString);
        var limit = c.Connection.QueryFirstOrDefault("select * from `limits` where `InternalName` = @internalName;", new
        {
            internalName
        });

        LimitCache.Add(internalName, limit);

        return limit;
    }

    public Limit GetByID(long id)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        var limit = c.Connection.QueryFirstOrDefault("select * from `limits` where `ID` = @id;", new
        {
            id
        });

        return limit;
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

    public int GetLimit(User usr, string internalName)
    {
        var limit = Get(internalName);
        var userLimit = DB.UserLimit.GetForUserAndLimit(usr.ID, limit.ID);

        if (userLimit != null)
            return userLimit.Value;

        var roleLimit = DB.RoleLimit.GetForRoleAndLimit(usr.RoleID, limit.ID);

        if (roleLimit != null)
            return roleLimit.Value;

        return 0;
    }
}