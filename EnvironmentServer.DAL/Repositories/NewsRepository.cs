﻿using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using System.Collections.Generic;

namespace EnvironmentServer.DAL.Repositories;

public class NewsRepository
{
    private Database DB;
    public NewsRepository(Database db)
    {
        DB = db;
    }

    public IEnumerable<News> GetLatest(int limit)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.Query<News>("select * from `news` Order BY Created DESC limit @limit;", new
        {
            limit = limit
        });
    }

    public void Insert(News news)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("INSERT INTO `news` (`Id`, `UserID`, `Content`, `Created`) " +
            "VALUES (NULL, @uid, @content, CURRENT_TIMESTAMP);", new
            {
                uid = news.UserID,
                content = news.Content
            });
    }

    public void Update(News news)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("UPDATE `news` SET `Content` = @content WHERE `news`.`Id` = @id;", new
        {
            id = news.ID,
            content = news.Content
        });
    }

    public void Delete(long id)
    {
        //DELETE FROM `news` WHERE `news`.`Id` = 1
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("DELETE FROM `news` WHERE `news`.`Id` = @id;", new
        {
            id = id
        });
    }
}