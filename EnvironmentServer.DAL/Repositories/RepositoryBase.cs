﻿using Dapper;
using EnvironmentServer.DAL.Interfaces;
using EnvironmentServer.DAL.Utility;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Repositories;

public abstract class RepositoryBase<T> where T : IDBIdentifier
{
    protected string TableName { get; }
    protected Database DB { get; }

    protected RepositoryBase(Database db, string tableName)
    {
        DB = db;
        TableName = tableName;
    }
    
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return await c.Connection.QueryAsync<T>($"select * from `{TableName}`;");
    }

    public async Task<T> GetByIDAsync(long id)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return await c.Connection.QuerySingleAsync<T>($"select * from `{TableName}` where ID = @id;", new
        {
            id
        });
    }

    public abstract void Insert(T t);

    public abstract void Update(T t);

    public void Delete(T t)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        c.Connection.Execute($"DELETE FROM {TableName} WHERE `Id` = @id;", new
        {
            id = t.ID
        });
    }
}