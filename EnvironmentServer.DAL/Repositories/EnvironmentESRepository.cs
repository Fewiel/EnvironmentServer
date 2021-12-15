using CliWrap;
using Dapper;
using Ductus.FluentDocker.Commands;
using Ductus.FluentDocker.Services;
using EnvironmentServer.DAL.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Repositories;

internal class EnvironmentESRepository
{
    private Database DB;

    public EnvironmentESRepository(Database db)
    {
        DB = db;
    }

    public IEnumerable<EnvironmentES> Get()
    {
        using var connection = DB.GetConnection();
        return connection.Query<EnvironmentES>("Select * from `environments_es`");
    }

    public IEnumerable<EnvironmentES> GetByID(long id)
    {
        using var connection = DB.GetConnection();
        return connection.Query<EnvironmentES>("Select * from `environments_es` where ID = @id", new
        {
            id = id
        });
    }

    public IEnumerable<EnvironmentES> GetByEnvironmentID(long id)
    {
        using var connection = DB.GetConnection();
        return connection.Query<EnvironmentES>("Select * from `environments_es` where EnvironmentID = @id", new
        {
            id = id
        });
    }

    public async Task AddAsync(long id, string esVersion)
    {
        int port = 9000;
        foreach (var es in Get())
        {
            if (es.Port != port)
                break;

            port += 1;
        }

        var envName = DB.Environments.Get(id).Name;

        var hosts = new Hosts().Discover();
        var _docker = hosts.FirstOrDefault(x => x.IsNative) ?? hosts.FirstOrDefault(x => x.Name == envName);
        var result = _docker.Host.Version(_docker.Certificates);
        var dID = _docker.Host.Run("elasticsearch:" + esVersion, null, _docker.Certificates).Data;
        var ps = _docker.Host.Ps(null, _docker.Certificates).Data;

        using var connection = DB.GetConnection();
        connection.Execute("INSERT INTO `environments_es` (`ID`, `EnvironmentID`, `ESVersion`, `Port`, `DockerID`, `Active`) " +
            "VALUES (NULL, @envID, @esVersion, @esPort, @dockerID, '1');", new
            {
                envID = id,
                esVersion = esVersion,
                esPort = port,
                dockerID = dID
            });
    }
}
