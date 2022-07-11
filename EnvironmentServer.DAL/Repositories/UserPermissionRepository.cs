using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using System.Collections.Generic;

namespace EnvironmentServer.DAL.Repositories;

public class UserPermissionRepository
{
    private Database DB;

    public UserPermissionRepository(Database db)
    {
        DB = db;
    }

    public IEnumerable<UserPermission> GetForUser(long id)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.Query<UserPermission>("select * from `users_permissions` where `UserID` = @id", new
        {
            id
        });
    }

    public void Add(UserPermission up)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("insert into `users_permissions` (`UserID`, `PermissionID`) values (@uid, @pid)", new
        {
            uid = up.UserID,
            pid = up.PermissionID
        });
    }

    public void Remove(UserPermission up)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("delete from `users_permissions` where `UserID` = @uid and `PermissionID` = @pid", new
        {
            uid = up.UserID,
            pid = up.PermissionID
        });
    }
}