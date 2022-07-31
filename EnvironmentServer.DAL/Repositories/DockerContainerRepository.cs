using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Repositories;

public class DockerContainerRepository
{
    private Database DB;

    public DockerContainerRepository(Database db)
    {
        DB = db;
    }

    //Get all
    public async Task<IEnumerable<DockerContainer>> GetAsync()
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return await c.Connection.QueryAsync<DockerContainer>("select * from `docker_containers`;");
    }

    //Get all for user
    public async Task<IEnumerable<DockerContainer>> GetAllForUserAsync(long uid)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return await c.Connection.QueryAsync<DockerContainer>("select * from `docker_containers` where UserID = @uid;", new
        {
            uid
        });
    }

    //Get by ID
    public async Task<IEnumerable<DockerContainer>> GetByIDAsync(long id)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return await c.Connection.QueryAsync<DockerContainer>("select * from `docker_containers` where ID = @id;", new
        {
            id
        });
    }

    //Get by File
    public async Task<IEnumerable<DockerContainer>> GetByFileIDAsync(long id)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return await c.Connection.QueryAsync<DockerContainer>("select * from `docker_containers` where DockerComposeFileID = @id;", new
        {
            id
        });
    }
}