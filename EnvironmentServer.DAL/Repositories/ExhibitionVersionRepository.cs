using Dapper;
using EnvironmentServer.DAL.Models;
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
        using var connection = DB.GetConnection();
        return connection.Query<ExhibitionVersion>("Select * from exhibition_versions");
    }
}
