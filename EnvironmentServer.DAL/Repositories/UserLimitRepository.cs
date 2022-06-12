using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using System.Collections.Generic;

namespace EnvironmentServer.DAL.Repositories;

public class UserLimitRepository
{
    private Database DB;

    public UserLimitRepository(Database db)
    {
        DB = db;
    }

    public IEnumerable<UserLimit> GetForUser(long id)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.Query<UserLimit>("select * from `users_limits` where `UserID` = @id", new
        {
            id
        });
    }

    public void Add(UserLimit ul)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("insert into `users_limits` (`UserID`, `LimitID`, `Value`) values (@uid, @lid, @value)", new
        {
            uid = ul.UserID,
            lid = ul.LimitID,
            value = ul.Value
        });
    }

    public void Update(UserLimit ul)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("update `users_limits` set `Value` = @value where `UserID` = @uid and `LimitID` = @lid", new
        {
            uid = ul.UserID,
            lid = ul.LimitID,
            value = ul.Value
        });
    }

    public void Remove(UserLimit ul)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("delete from `users_limits` where `UserID` = @uid and `LimitID` = @lid", new
        {
            uid = ul.UserID,
            lid = ul.LimitID
        });
    }
}