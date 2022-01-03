using Dapper;
using EnvironmentServer.DAL.Models;
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
        using var connection = DB.GetConnection();
        return connection.QuerySingleOrDefault<Department>("select * from departments where ID = @id;", new
        {
            id = id
        });
    }

    public IEnumerable<Department> GetAll()
    {
        using var connection = DB.GetConnection();
        return connection.Query<Department>("select * from departments;");
    }

    public void Insert(Department dp)
    {
        using var connection = DB.GetConnection();
        connection.Execute("insert into `departments` (ID, Name, Description) values (NULL, @name, @description);", new
        {
            name = dp.Name,
            description = dp.Description
        });
    }

    public void Update(Department dp)
    {
        using var connection = DB.GetConnection();
        connection.Execute("UPDATE `departments` SET " +
            "`Name` = @name, " +
            "`Description` = @description", new
        {
            name = dp.Name,
            description = dp.Description
        });
    }
}
