using Dapper;
using EnvironmentServer.DAL.Models;
using System.Collections.Generic;

namespace EnvironmentServer.DAL.Repositories;

public class NewsRepository
{
    private Database DB;
    public NewsRepository(Database db)
    {
        DB = db;
    }

    public IEnumerable<News> GetLatest()
    {
        using var connection = DB.GetConnection();
        return connection.Query<News>("select * from `news` Order BY Created limit 10;");
    }

    public void Insert(News news)
    {
        using var connection = DB.GetConnection();
        connection.Execute("INSERT INTO `news` (`Id`, `UserID`, `Content`, `Created`) " +
            "VALUES (NULL, @uid, @content, CURRENT_TIMESTAMP);", new
        {
            uid = news.UserID,
            content = news.Content
        });
    }

    public void Update(News news)
    {
        using var connection = DB.GetConnection();
        connection.Execute("UPDATE `news` SET `Content` = @content WHERE `news`.`Id` = @id;", new
            {
                id = news.ID,
                content = news.Content
            });
    }
}
