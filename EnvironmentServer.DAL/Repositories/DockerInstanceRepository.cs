using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using System.Collections.Generic;

namespace EnvironmentServer.DAL.Repositories
{
    public class DockerInstanceRepository
    {
        private Database DB;

        public DockerInstanceRepository(Database db)
        {
            DB = db;
        }

        public IEnumerable<DockerInstance> GetAll()
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            return c.Connection.Query<DockerInstance>("Select * from `docker_instances`");
        }

        public IEnumerable<DockerInstance> GetByID(long id)
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            return c.Connection.Query<DockerInstance>("Select * from `docker_instances` where ID = @id", new
            {
                id
            });
        }

        public IEnumerable<DockerInstance> GetByEnvironmentID(long id)
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            return c.Connection.Query<DockerInstance>("Select * from `docker_instances` where EnvironmentID = @id", new
            {
                id
            });
        }
        //INSERT INTO `environments_es` (`ID`, `EnvironmentID`, `ESVersion`, `Port`, `DockerID`, `Active`) VALUES (NULL, @envID, @esVersion, @esPort, @dockerID, '1');
        public long Add(DockerInstance di)
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            return c.Connection.QuerySingle<int>("Insert into `docker_instances` (`Name`, `EnvironmentID`, `Image`)" +
                " Values (@name, @environmentID, @image); SELECT LAST_INSERT_ID();", new
                {
                    name = di.Name,
                    environmentID = di.EnvironmentID,
                    image = di.Image
                });
        }

        public void Update(DockerInstance di)
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            c.Connection.Execute("Update `docker_instances` set `InstanceID` = @iid," +
                "`Name` = @name, `Port` = @port, `Interactive` = @interactive, " +
                "`DockerEnvironment` = @denv, `PortMappings` = @pmappings, `Running` = @running", new
                {
                    iid = di.InstanceID,
                    name = di.Name,
                    port = di.Port,
                    interactive = di.Interactive,
                    denv = di.DockerEnvironment, 
                    pmappings = di.PortMappings,
                    running = di.Running
                });
        }

        public void Delete(long id)
        {
            using var c = new MySQLConnectionWrapper(DB.ConnString);
            c.Connection.Execute("Delete from `docker_instances` where ID = @id", new
            {
                id
            });
        }
    }
}