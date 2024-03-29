﻿using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using System.Collections.Generic;
using System.Linq;

namespace EnvironmentServer.DAL.Repositories;

public class RolePermissionRepository
{
    private Database DB;

    public RolePermissionRepository(Database db)
    {
        DB = db;
    }

    public IEnumerable<RolePermission> GetForRole(long rid)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.Query<RolePermission>("select * from `role_permissions` where `RoleID` = @rid", new
        {
            rid
        });
    }

    public RolePermission GetForRoleAndPermission(long rid, long pid)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.QuerySingleOrDefault<RolePermission>("select * from `role_permissions` where `RoleID` = @rid and `PermissionID` = @pid", new
        {
            rid,
            pid
        });
    }

    public void Add(RolePermission rp)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("insert into `role_permissions` (`RoleID`, `PermissionID`) values (@rid, @pid)", new
        {
            rid = rp.RoleID,
            pid = rp.PermissionID
        });
    }

    public void Remove(RolePermission rp)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("delete from `role_permissions` where `RoleID` = @rid and `PermissionID` = @pid", new
        {
            rid = rp.RoleID,
            pid = rp.PermissionID
        });
    }
}