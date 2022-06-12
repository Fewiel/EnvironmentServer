using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using System.Collections.Generic;

namespace EnvironmentServer.DAL.Repositories;

public class PermissionRepository
{
    private Database DB;

    public PermissionRepository(Database db)
    {
        DB = db;
    }

    public IEnumerable<Permission> GetAll()
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.Query<Permission>("select * from `permissions`");
    }

    public Permission Get(string internalName)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.QuerySingleOrDefault<Permission>("select * from `permissions` where `InternalName` = @internalName", new
        {
            internalName
        });
    }

    public void Add(Permission p)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("insert into `permissions` (`Name`, `InternalName`) values (@name, @internalName)", new
        {
            name = p.Name,
            internalName = p.InternalName
        });
    }

    public void Update(Permission p)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("update `permissions` set `Name` = @name, `InternalName` = @internalName where `ID` = @id", new
        {
            id = p.ID,
            name = p.Name,
            internalName = p.InternalName
        });
    }

    public void Delete(long id)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("delete from `permissions` where `ID` = @id", new
        {
            id
        });
    }
}