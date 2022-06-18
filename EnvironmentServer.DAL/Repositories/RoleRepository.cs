using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using System.Collections.Generic;

namespace EnvironmentServer.DAL.Repositories;

public class RoleRepository
{
    private Database DB;

    public RoleRepository(Database db)
    {
        DB = db;
    }

    public IEnumerable<Role> GetAll()
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.Query<Role>("select * from `roles`;");
    }

    public Role GetByID(long id)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.QuerySingleOrDefault<Role>("select * from `roles` where `ID` = @id", new
        {
            id
        });
    }

    public Role GetByName(string name)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.QuerySingleOrDefault<Role>("select * from `roles` where `Name` = @name", new
        {
            name
        });
    }

    public long Add(Role r)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.QuerySingle<int>("insert into `roles` (Name, Description) values (@name, @desc); SELECT LAST_INSERT_ID();", new
        {
            name = r.Name,
            desc = r.Description
        });
    }

    public void Update(Role r)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("update `roles` set `Name` = @name, `Description` = @desc where `ID` = @id", new
        {
            id = r.ID,
            name = r.Name,
            desc = r.Description
        });
    }

    public void ClearPermissions(long id)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("delete from `role_permissions` where `RoleID` = @id", new
        {
            id
        });
    }

    public void ClearLimits(long id)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("delete from `role_limits` where `RoleID` = @id", new
        {
            id
        });
    }

    public void Delete(long id)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("delete from `roles` where `ID` = @id", new
        {
            id
        });
    }
}