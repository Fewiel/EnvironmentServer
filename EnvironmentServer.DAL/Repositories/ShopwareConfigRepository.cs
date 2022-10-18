using Dapper;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.DAL.Utility;
using System;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Repositories;

public class ShopwareConfigRepository : RepositoryBase<ShopwareConfig>
{
    public ShopwareConfigRepository(Database db) : base(db, "shopware_config")
    {
    }

    public override async Task InsertAsync(ShopwareConfig t)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        await c.Connection.ExecuteAsync("insert into `shopware_config` (`EnvID`, `Content`) " +
            "values (@envID, @content)", new
            {
                envID = t.EnvID,
                content = t.Content
            });
    }

    public override async Task UpdateAsync(ShopwareConfig t)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        await c.Connection.ExecuteAsync("update `shopware_config` set `Content` = @content where `EnvID` = @envID;", new
        {
            envID = t.EnvID,
            content = t.Content
        });
    }

    public async Task<ShopwareConfig> GetByEnvIDAsync(long id)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        return await c.Connection.QueryFirstOrDefaultAsync<ShopwareConfig>("select * from `shopware_config` where EnvID = @id", new
        {
            id
        });
    }
}