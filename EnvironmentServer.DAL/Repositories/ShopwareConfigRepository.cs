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
        await c.Connection.ExecuteAsync("insert into `shopware_config` (`EnvID`, `Content`, `LatestUpdate`) " +
            "values (@envID, @content, @latestupdate)", new
            {
                envID = t.EnvID,
                content = t.Content,
                latestupdate = DateTime.Now.ToString("YYYY-MM-DD HH:MI:SS")
            });
    }

    public override async Task UpdateAsync(ShopwareConfig t)
    {
        using var c = new MySQLConnectionWrapper(DB.ConnString);
        await c.Connection.ExecuteAsync("update `shopware_config` set `EnvID` = @envID, `Content` = @content, `LatestUpdate` = @latestupdate where `ID` = @id;", new
        {
            id = t.ID,
            envID = t.EnvID,
            content = t.Content,
            latestupdate = DateTime.Now.ToString("YYYY-MM-DD HH:MI:SS")
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