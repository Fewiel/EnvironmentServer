using CliWrap;
using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Repositories;

public class EnvironmentESRepository
{
    private Database DB;

    public EnvironmentESRepository(Database db)
    {
        DB = db;
    }

    public IEnumerable<EnvironmentES> Get()
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.Query<EnvironmentES>("Select * from `environments_es`");
    }

    public EnvironmentES GetByID(long id)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.QuerySingleOrDefault<EnvironmentES>("Select * from `environments_es` where ID = @id", new
        {
            id = id
        });
    }
    public EnvironmentES GetByDockerID(string dockerID)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.QuerySingleOrDefault<EnvironmentES>("Select * from `environments_es` where DockerID = @id", new
        {
            id = dockerID
        });
    }

    public EnvironmentES GetByEnvironmentID(long id)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.QuerySingleOrDefault<EnvironmentES>("Select * from `environments_es` where EnvironmentID = @id", new
        {
            id = id
        });
    }

    public async Task AddAsync(long id, string esVersion)
    {
        int port = 9000;
        port += Get().Count();

        var envName = DB.Environments.Get(id).InternalName;
        var dID = "es_docker_" + envName + "_uid_" + DB.Environments.Get(id).UserID;
        await Cli.Wrap("/bin/bash")
            .WithArguments($"-c \"docker run -d --name {dID} -p 127.0.0.1:{port}:9200 -p 127.0.0.1:{port + 100}:9300 -e \"discovery.type=single-node\" -it docker.elastic.co/elasticsearch/elasticsearch:{esVersion}\"")
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();


        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("INSERT INTO `environments_es` (`ID`, `EnvironmentID`, `ESVersion`, `Port`, `DockerID`, `Active`) " +
            "VALUES (NULL, @envID, @esVersion, @esPort, @dockerID, '1');", new
            {
                envID = id,
                esVersion = esVersion,
                esPort = port,
                dockerID = dID
            });
    }

    public async Task StartContainer(string dockerID)
    {
        try
        {
            await Cli.Wrap("/bin/bash")
            .WithArguments($"-c \"docker start {dockerID}\"")
            .ExecuteAsync();

            using var c = new MySQLConnectionWrapper(DB.ConnString);
            c.Connection.Execute("UPDATE `environments_es` SET `Active` = '1' WHERE `environments_es`.`DockerID` = @dID;", new
            {
                dID = dockerID
            });
        }
        catch (Exception ex)
        {
            DB.Logs.Add("ESCleanup", "StartContainer() - Startup Error - " + ex.ToString());
        }        
    }

    public async Task StopContainer(string dockerID)
    {
        try
        {
            await Cli.Wrap("/bin/bash")
            .WithArguments($"-c \"docker stop {dockerID}\"")
            .ExecuteAsync();
        }
        catch (Exception ex)
        {
            DB.Logs.Add("ESCleanup", "StopContainer() - Nothing to Stop");
        }

        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("UPDATE `environments_es` SET `Active` = '0' WHERE `environments_es`.`DockerID` = @dID;", new
        {
            dID = dockerID
        });
    }

    public async Task StopAll()
    {
        try
        {
            await Cli.Wrap("/bin/bash")
            .WithArguments($"-c \"docker kill $(docker ps -q)\"")
            .ExecuteAsync();
        }
        catch (Exception ex)
        {
            DB.Logs.Add("ESCleanup", "StopAll() - Nothing to Stop");
        }

        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("UPDATE `environments_es` SET `Active` = '0';");
    }

    public async Task Remove(string dockerID)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        var es = GetByDockerID(dockerID);
        await Cli.Wrap("/bin/bash")
            .WithArguments($"-c \"docker rm -f {es.DockerID}\"")
            .ExecuteAsync();
        c.Connection.Execute($"DELETE FROM `environments_es` where id = {es.ID}");
    }

    public async Task Cleanup()
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        var es_list = c.Connection.Query<EnvironmentES>("Select * from `environments_es` where LastUse < DATE(DATE_SUB(NOW(), INTERVAL 30 DAY));");

        try
        {
            foreach (var es in es_list)
            {
                await Cli.Wrap("/bin/bash")
                    .WithArguments($"-c \"docker rm -f {es.DockerID}\"")
                    .ExecuteAsync();
                c.Connection.Execute($"DELETE FROM `environments_es` where id = {es.ID}");
            }
        }
        catch (Exception ex)
        {
            DB.Logs.Add("ESCleanup", "Cleanup() - Nothing to Cleanup");
        }

    }
}
