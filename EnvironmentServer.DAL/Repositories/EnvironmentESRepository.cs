using CliWrap;
using Dapper;
using EnvironmentServer.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Repositories
{
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

            var stdOutBuffer = new StringBuilder();
            var stdErrBuffer = new StringBuilder();
            await Cli.Wrap("/bin/bash")
                .WithArguments($"-c \"docker run -d --name {envName} -p {port}:{port} -p {port + 100}:{port + 100} -e \"discovery.type = single - node\" elasticsearch:{esVersion}\"")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .ExecuteAsync();
            var stdOut = stdOutBuffer.ToString();
            var stdErr = stdErrBuffer.ToString();

            if (!string.IsNullOrEmpty(stdErr))
            {
                DB.Logs.Add("DAL", "ERROR - EnvironmentESRepository on Add() - " + stdErr);
                return;
            }

            using var connection = DB.GetConnection();
            connection.Execute("INSERT INTO `environments_es` (`ID`, `EnvironmentID`, `ESVersion`, `Port`, `DockerID`, `Active`) " +
                "VALUES (NULL, @envID, @esVersion, @esPort, @dockerID, '1');", new
                {
                    envID = id,
                    esVersion = esVersion,
                    esPort = port,
                    dockerID = stdOut
                });
        }
    }
}
