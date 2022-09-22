using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Repositories;

public class DockerContainerRepository : RepositoryBase<DockerContainer>
{
    public DockerContainerRepository(Database db) : base(db, "docker_containers") { }

    public async Task<IEnumerable<DockerContainer>> GetAllForUserAsync(long uid)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return await c.Connection.QueryAsync<DockerContainer>("select * from `docker_containers` where UserID = @uid;", new
        {
            uid
        });
    }

    public async Task<IEnumerable<DockerContainer>> GetByFileIDAsync(long id)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return await c.Connection.QueryAsync<DockerContainer>("select * from `docker_containers` where DockerComposeFileID = @id;", new
        {
            id
        });
    }

    public async Task<DockerContainer> GetByDockerIDAsync(string id)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return await c.Connection.QuerySingleAsync<DockerContainer>("select * from `docker_containers` where DockerID = @id;", new
        {
            id
        });
    }

    public async Task<int> GetByDockerCountForUser(long uid)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return await c.Connection.QuerySingleAsync<int>("select COUNT(*) from `docker_containers` where UserID = @uid;", new
        {
            uid
        });
    }


    public override async Task<int> InsertAsync(DockerContainer t)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return await c.Connection.QuerySingleAsync<int>("insert into `docker_containers` (`UserID`, `Name`, `DockerComposeFileID`) values " +
            "(@uid, @name, @dcfid); SELECT LAST_INSERT_ID();", new
            {
                uid = t.UserID,
                name = t.Name,
                dcfid = t.DockerComposeFileID
            });
    }

    public override async Task UpdateAsync(DockerContainer t)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        await c.Connection.ExecuteAsync("update `docker_containers` set " +
            "`DockerID` = @did, `Name` = @name, `Active` = @active where ID = @id;", new
            {
                id = t.ID,
                did = t.DockerID,
                name = t.Name,
                active = t.Active
            });
    }
}