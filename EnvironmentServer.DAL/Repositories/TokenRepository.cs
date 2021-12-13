using Dapper;
using EnvironmentServer.DAL.Models;
using System;
using System.Collections;

namespace EnvironmentServer.DAL.Repositories;

public class TokenRepository
{
    private readonly Database DB;

    public TokenRepository(Database db)
    {
        DB = db;
    }

    public Guid Generate(long userID)
    {
        var newGuid = Guid.NewGuid();
        using var connection = DB.GetConnection();
        connection.Execute("INSERT INTO `token` (`ID`, `Guid`, `UserID`, `Used`, `Created`) " +
            "VALUES (NULL, @guid, @userid, '0', CURRENT_TIMESTAMP);", new
            {
                guid = newGuid,
                userid = userID
            });
        return newGuid;
    }

    public bool Use(Guid guid, long userid)
    {
        using var connection = DB.GetConnection();
        var token = connection.QuerySingleOrDefault<Token>("Select * from `token` where Guid = @guid and UserID = @userid and Used = 0;", new
        {
            guid = guid,
            userid = userid
        });

        if (token == null)
            return false;

        connection.Execute("UPDATE `token` SET `Used` = '1' WHERE `token`.`ID` = @id;", new
        {
            id = token.ID
        });

        return true;
    }

    public void DeleteOldTokens()
    {
        using var connection = DB.GetConnection();
        connection.Execute($"UPDATE `token` SET `Used` = '1' where Created < DATE(DATE_SUB(NOW(), INTERVAL 4 HOUR));");
        connection.Execute($"DELETE FROM `token` where Created < DATE(DATE_SUB(NOW(), INTERVAL 48 HOUR));");
    }
}
