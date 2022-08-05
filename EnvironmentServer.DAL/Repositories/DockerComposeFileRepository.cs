using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Repositories;

public class DockerComposeFileRepository : RepositoryBase<DockerComposeFile>
{
    public DockerComposeFileRepository(Database db) : base(db, "docker_composer_files") { }

    public override void Insert(DockerComposeFile t)
    {
        throw new System.NotImplementedException();
    }

    public override void Update(DockerComposeFile t)
    {
        throw new System.NotImplementedException();
    }
}