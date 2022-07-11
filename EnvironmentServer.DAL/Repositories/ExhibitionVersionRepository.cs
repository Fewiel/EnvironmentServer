using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using System.Collections.Generic;

namespace EnvironmentServer.DAL.Repositories;

public class ExhibitionVersionRepository
{
    private Database DB;

    public ExhibitionVersionRepository(Database db)
    {
        DB = db;
    }

    public IEnumerable<ExhibitionVersion> Get()
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.Query<ExhibitionVersion>("Select * from exhibition_versions");
    }
}
