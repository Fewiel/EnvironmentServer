using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Repositories;

public class DockerComposeFileRepository : RepositoryBase<DockerComposeFile>
{
    public DockerComposeFileRepository(Database db) : base(db, "docker_composer_files") { }

    public async Task<IEnumerable<DockerComposeFile>> GetForUser(long id)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return await c.Connection.QueryAsync<DockerComposeFile>("select * from `docker_compose_files` where UserID = @id;", new
        {
            id
        });
    }

    public override async Task InsertAsync(DockerComposeFile t)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        await c.Connection.ExecuteAsync("insert into `docker_compose_files` (`UserID`, `Name`, `Description`, `FileContent`) " +
            "values (@uid, @name, @desc, @cont)", new
        {
            uid = t.UserID,
            name = t.Name,
            desc = t.Description,
            cont = t.FileContent
        });
    }

    public override async Task UpdateAsync(DockerComposeFile t)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        await c.Connection.ExecuteAsync("update `docker_compose_files` set " +
            "`UserID` = @uid, `Name` = @name, `Description` = @desc, `FileContent` = @cont where ID = @id;", new
            {
                id = t.ID,
                uid = t.UserID,
                name = t.Name,
                desc = t.Description,
                cont = t.FileContent
            });
    }
}