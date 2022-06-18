using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using System.Collections.Generic;

namespace EnvironmentServer.DAL.Repositories;

public class PermissionRepository
{
    private readonly Database DB;
    private readonly Dictionary<string, Permission> PermissionCache = new();

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
        if (PermissionCache.TryGetValue(internalName, out var value))
            return value;

        using var c = new MySQLConnectionWrapper(DB.ConnString);
        var permission = c.Connection.QuerySingleOrDefault<Permission>("select * from `permissions` where `InternalName` = @internalName", new
        {
            internalName
        });

        PermissionCache.Add(internalName, permission);

        return permission;
    }

    public Permission GetByID(long id)
    {

        using var c = new MySQLConnectionWrapper(DB.ConnString);
        var permission = c.Connection.QuerySingleOrDefault<Permission>("select * from `permissions` where `ID` = @id", new
        {
            id
        });

        return permission;
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

    public bool HasPermission(User usr, string internalName)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        var pid = Get(internalName).ID;

        var hasUserPermission = c.Connection.ExecuteScalar<bool>("select Count(1) from `users_permissions` where `UserID` = @uid and `PermissionID` = @pid limit 1", new
        {
            uid = usr.ID,
            pid
        });

        if (hasUserPermission)
            return true;

        if (usr.RoleID == 0)
            return false;

        var hasPermission = c.Connection.ExecuteScalar<bool>("select Count(1) from `role_permissions` where `RoleID` = @rid and `PermissionID` = @pid limit 1", new
        {
            uid = usr.RoleID,
            pid
        });

        return hasPermission;
    }
}