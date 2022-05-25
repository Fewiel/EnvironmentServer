using Dapper;
using EnvironmentServer.DAL.Models;
using Microsoft.VisualBasic.FileIO;
using System.Collections.Generic;

namespace EnvironmentServer.DAL.Repositories;

public class TemplateRepository
{
    private Database DB;

    public TemplateRepository(Database db)
    {
        DB = db;
    }

    public long Create(Template template)
    {
        using var connection = DB.GetConnection();
        return connection.QuerySingle<int>("Insert into `templates` (`ID`, `Name`, `Description`, " +
            "`UserID`, `FastDeploy`, `ShopwareVersion`, `Created`) values " +
            "(NULL, @name, @desc, @userid, @fd, @sv, CURRENT_TIMESTAMP); SELECT LAST_INSERT_ID();", new
            {
                name = template.Name,
                desc = template.Description,
                userid = template.UserID,
                fd = template.FastDeploy,
                sv = template.ShopwareVersion
            });
    }

    public IEnumerable<Template> GetAll()
    {
        using var connection = DB.GetConnection();
        return connection.Query<Template>("select * from `templates`;");
    }

    public Template Get(long id)
    {
        using var connection = DB.GetConnection();
        return connection.QuerySingle<Template>("select * from `templates` where ID = @id;", new
        {
            id
        });
    }

    public Template GetForFastDeploy(string v)
    {
        using var connection = DB.GetConnection();
        return connection.QuerySingle<Template>("SELECT * FROM `templates` where ShopwareVersion = @v and FastDeploy = 1;", new
        {
            v
        });
    }

    public void Delete(long id)
    {
        using var connection = DB.GetConnection();
        connection.Execute("Delete from `templates` where ID = @id`", new
        {
            id
        });
    }
}