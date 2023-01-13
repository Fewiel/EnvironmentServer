using EnvironmentServer.DAL.Models;

namespace EnvironmentServer.DAL.Repositories;

public class Shopware6VersionRepository
{
	private Database DB;

	public Shopware6VersionRepository(Database db)
	{
		DB = db;
	}


}
