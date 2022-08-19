using Dapper;
using EnvironmentServer.DAL.Interfaces;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Repositories;

public class DockerPortRepository
{
    private Database DB;

    public DockerPortRepository(Database db)
    {
        DB = db;
    }

    //Select All
    public IEnumerable<DockerPort> Get()
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.Query<DockerPort>("select * from `docker_ports`;");
    }

    //Select for File
    public IEnumerable<DockerPort> GetForContainer(long id)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.Query<DockerPort>("select * from `docker_ports` where `DockerContainerID` = @id;", new
        {
            id
        });
    }

    //Insert
    public void Insert(DockerPort dp)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("insert into `docker_ports` (`Port`, `DockerContainerID`) values (@port, @dfid)", new
        {
            port = dp.Port,
            dfid = dp.DockerContainerID
        });
    }
}