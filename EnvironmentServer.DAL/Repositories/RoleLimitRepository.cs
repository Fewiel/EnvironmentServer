using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using System.Collections.Generic;

namespace EnvironmentServer.DAL.Repositories;

public class RoleLimitRepository
{
    private Database DB;

    public RoleLimitRepository(Database db)
    {
        DB = db;
    }

    public IEnumerable<RoleLimit> GetForRole(long roleID)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.Query<RoleLimit>("select * from `role_limits` where `RoleID` = @roleID", new
        {
            roleID
        });
    }

    public void Add(RoleLimit rl)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("inset into `rule_limits` (`RoleID`, `LimitID`, `Value`) values (@rid, @lid, @value)", new
        {
            rid = rl.RoleID,
            lid = rl.LimitID,
            value = rl.Value
        });
    }

    public void Update(RoleLimit rl)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("update `role_limits` set `Value` @value where `RoleID` = @rid and `LimitID` = @lid", new
        {
            rid = rl.RoleID,
            lid = rl.LimitID,
            value = rl.Value
        });
    }

    public void Remove(RoleLimit rl)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("delete from `role_limits` where `RoleID` = @rid and `LimitID` = @lid)", new
        {
            rid = rl.RoleID,
            lid = rl.LimitID
        });
    }

    public void RemoveForRole(long rid)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("delete from `role_limits` where `RoleID` = @rid)", new
        {
            rid
        });
    }
}
