using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using System.Collections.Generic;

namespace EnvironmentServer.DAL.Repositories;

public class DepartmentRepository
{
    private Database DB;

    public DepartmentRepository(Database db)
    {
        DB = db;
    }

    public Department Get(long id)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.QuerySingleOrDefault<Department>("select * from departments where ID = @id;", new
        {
            id = id
        });
    }

    public IEnumerable<Department> GetAll()
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return c.Connection.Query<Department>("select * from departments;");
    }

    public void Insert(Department dp)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("insert into `departments` (ID, Name, Description) values (NULL, @name, @description);", new
        {
            name = dp.Name,
            description = dp.Description
        });
    }

    public void Update(Department dp)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute("UPDATE `departments` SET " +
            "`Name` = @name, " +
            "`Description` = @description", new
        {
            name = dp.Name,
            description = dp.Description
        });
    }
}
